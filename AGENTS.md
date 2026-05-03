# vega-backend/AGENTS.md — Backend Agent Rules

> Scope: `vega-backend/` only. Read [../AGENTS.md](../AGENTS.md) first.

---

## Project Structure

```
vega-backend/src/
├── Core/
│   ├── CleanArc.Domain/
│   │   ├── Common/BaseEntity.cs       # BaseEntity<TKey>, IEntity, ITimeModification
│   │   └── Entities/                  # Domain entities grouped by bounded context
│   └── CleanArc.Application/
│       ├── Features/                  # CQRS handlers organized by entity
│       ├── Contracts/                 # Interfaces: IUnitOfWork, IRepository<T>, AI contracts
│       ├── Models/Common/             # OperationResult<T>
│       └── Profiles/                  # AutoMapper profiles
├── Infrastructure/
│   ├── CleanArc.Infrastructure.Persistence/
│   │   ├── ApplicationDbContext.cs    # EF Core DbContext
│   │   ├── Repositories/             # Concrete repository implementations
│   │   ├── SeedDatabaseService/      # SeedGameData.cs (idempotent seed)
│   │   └── Services/AI/              # AI pipeline, prompt registry, rate limiting
│   └── CleanArc.Infrastructure.Identity/
│       ├── Identity/                  # ASP.NET Core Identity config
│       └── Jwt/                       # JWT service
└── API/
    └── CleanArc.Web.Api/
        ├── Modules/                   # Carter ICarterModule endpoints
        ├── Controllers/               # Legacy MVC controllers (UserManagement etc.)
        └── Program.cs                 # Bootstrap
```

---

## Layer Dependency Rules

```
API → Application → Domain     (allowed)
Infrastructure → Domain        (allowed)
Infrastructure → Application   (allowed — implements contracts)
Domain → anything else         (FORBIDDEN)
Application → Infrastructure   (FORBIDDEN — use interfaces only)
```

---

## CQRS Command/Query Pattern

Every handler lives in `CleanArc.Application/Features/<Entity>/Commands/` or `/Queries/`.

### Command template

```csharp
// Command record and handler in the SAME file
public sealed record DoSomethingCommand(int Id, string Payload) : IRequest<OperationResult<bool>>;

internal sealed class DoSomethingCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DoSomethingCommand, OperationResult<bool>>
{
    public async ValueTask<OperationResult<bool>> Handle(
        DoSomethingCommand request, CancellationToken cancellationToken)
    {
        var entity = await unitOfWork.FooRepository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.NotFoundResult("Entity not found");

        // business logic
        await unitOfWork.FooRepository.UpdateAsync(entity);
        return OperationResult<bool>.SuccessResult(true);
    }
}
```

**Rules:**
- Records are `sealed`. Handlers are `internal sealed`.
- Inject `IUnitOfWork` (never `ApplicationDbContext` directly).
- Return `OperationResult<T>` for all Commands. Queries may return the DTO directly.
- Use `OperationResult<T>.NotFoundResult`, `UnauthorizedResult`, `ForbiddenResult`, or `FailureResult` — not raw exceptions.

---

## Carter Endpoint Pattern

```csharp
public sealed class FooEndpoints : ICarterModule
{
    private const string RoutePrefix = "/api/v{version:apiVersion}/foo/";
    private const double Version = 1.1;
    private const string Tag = "Foo Management";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(builder => builder.MapGet(
            $"{RoutePrefix}bar",
            async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            {
                var userId = int.Parse(user.Identity!.GetUserId());
                var result = await sender.Send(new GetFooQuery(userId), ct);
                return result.ToEndpointResult();
            }), Version, "GetFoo", Tag)
          .RequireAuthorization(b => b.RequireRole("teacher", "admin"));
    }

    // Request records go here (inside the endpoint file)
    public sealed record CreateFooRequest(string Name);
}
```

**Rules:**
- New endpoints: use `ICarterModule` at API version `1.1`.
- Always attach `.RequireAuthorization(...)` with explicit roles.
- User ID is extracted via `user.Identity!.GetUserId()` (extension in `CleanArc.SharedKernel`).
- `result.ToEndpointResult()` maps `OperationResult<T>` to the correct HTTP status automatically.

---

## OperationResult<T> — Result Monad

`OperationResult<T>` (`CleanArc.Application.Models.Common`) is the **only** way handlers communicate success/failure.

| Factory method | HTTP mapping |
|---|---|
| `SuccessResult(value)` | 200 OK |
| `FailureResult(message)` | 400 Bad Request |
| `NotFoundResult(message)` | 404 Not Found |
| `UnauthorizedResult(message)` | 401 Unauthorized |
| `ForbiddenResult(message)` | 403 Forbidden |

---

## Domain Entity Pattern

All domain entities extend `BaseEntity` (int PK):

```csharp
public class Foo : BaseEntity  // PK = int Id, CreatedTime, ModifiedDate auto-set
{
    public string Name { get; set; }
}

// For non-int keys:
public class Bar : BaseEntity<Guid> { }
```

`ITimeModification` (`CreatedTime`, `ModifiedDate`) is auto-populated in `ApplicationDbContext.OnSavingChanges`.

**Rules:**
- No domain entity imports EF Core, Identity, or application namespaces.
- Entity configurations (`IEntityTypeConfiguration<T>`) go in `Infrastructure/Configuration/`.
- EF conventions applied globally: pluralized table names, restrict-delete behavior, all `IEntity` types auto-registered.

---

## Authentication & Authorization

### Educators / Admins
- ASP.NET Core Identity + JWT Bearer tokens.
- Roles: `"teacher"`, `"admin"`.
- Token issued by `JwtService`, refreshed via `/Users/RefreshSignIn`.

### Students
- Visual-sequence login: student submits `loginCode` + `visualSequence` (emoji icon IDs).
- Backend verifies with `BCrypt` against `StudentCredential.HashedVisualPassword`.
- Role in JWT: `"student"`.

**Never** expose `HashedVisualPassword`, raw password reset tokens, or `ExternalUuid` in API responses.

---

## AI Feature Pattern (Backend)

```
Request
  └─► Carter endpoint
        └─► Mediator → Handler (Application layer)
              └─► IChallengeOrchestrator (contract)
                    └─► ChallengeAiPipelineService (Infrastructure)
                          ├─► AiPromptRegistry.Get(useCase, variant)
                          ├─► GoogleAiService / OllamaChallengeOrchestrator
                          ├─► AiRateLimitService (per-user rate limiting)
                          └─► AiAuditService (logs every AI call to AiAuditLog)
```

### AiPromptRegistry rules
- **All prompts live in `AiPromptRegistry.cs`** — never inline prompts in services or handlers.
- Every prompt must output `PURE JSON ONLY. NO MARKDOWN. NO COMMENTS.`
- Prompts are keyed by `AiUseCases.*` constants.
- Adding a new use case: add a `AiUseCases` constant, handle it in the switch, write a unit-testable prompt string.

### AiUsageLog / AiAuditLog
- Every AI call must be recorded via `AiUsageService` and `AiAuditService`.
- Do not bypass these services with a direct HTTP call to the AI provider.

---

## Seeding Pattern

Seeding is handled by `SeedGameData : ISeedGameData` in `Infrastructure.Persistence`.

**Rules:**
- Check existence before inserting: `if (!await _dbContext.Foo.AnyAsync()) { ... }`.
- For upsert-style seeds (e.g., badges): load existing by key, update fields, set a `hasChanges` flag, save once at the end.
- `ContentData` on `Challenge` is stored as **camelCase JSON** using the static `_camelCase` `JsonSerializerOptions`. Always serialize with `JsonSerializer.Serialize(obj, _camelCase)`.
- Firebase Storage URLs in seed data use the pattern: `https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/<path>?alt=media`.
- Image ref paths (relative, for frontend resolution): `"badges/filename.png"`, `"quizzes/word-twins/cat.jpg"`.

---

## Logging

- Serilog is globally configured. Inject `ILogger<T>` into handlers.
- Do **not** use `Console.WriteLine` — it bypasses structured logging.
- Error handler in `Program.cs` returns `{ success: false, message: "Server Error" }` — do not expose stack traces.

---

## Absolute Prohibitions

- ❌ Do **not** inject `ApplicationDbContext` into Application layer handlers.
- ❌ Do **not** add external NuGet packages without explicit user approval.
- ❌ Do **not** inline AI prompts outside `AiPromptRegistry`.
- ❌ Do **not** return raw `IdentityResult` or EF entities directly from endpoints — use DTOs.
- ❌ Do **not** leave temporary `.sql`, `.http`, or test log files in the repo.
- ❌ Do **not** create Controllers for new features — use Carter modules.

---

## TODO / Uncertain Points

- **`(teacher)` vs `(educator)` route group:** The frontend has both. The distinction is unclear from code inspection alone. Treat them as equivalent until confirmed. _TODO: clarify with project owner._
- **gRPC plugin (`Plugins/`):** Grpc plugin services are registered in `Program.cs` but not deeply inspected. _TODO: document gRPC contracts if agents need to modify them._

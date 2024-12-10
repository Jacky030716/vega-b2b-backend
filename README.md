# MemowordCleanArc

Expanded Guidelines for Creating a New API
1. Define the Requirements
Step 1.1: Clarify the purpose of the API.
Example: Create an API for managing "Products" with operations like adding, updating, and retrieving products.
Step 1.2: Decide on the route structure and HTTP method.
Example: POST /api/v1/products for adding a product.
Step 1.3: Define the input and output JSON structure.
2. Update the Project Structure
Step 2.1: Add a New Folder for the Feature

Navigate to Core/CleanArc.Application/Features.
Create a new folder named Products to group all files related to this feature.
mathematica
Copy code
Core/CleanArc.Application/Features/Products
Step 2.2: Inside the Products folder, create the following structure:

Commands: For command handling (e.g., AddProductCommand).
Queries: For query handling (e.g., GetProductQuery).
DTOs: For data transfer objects.
mathematica
Copy code
Core/CleanArc.Application/Features/Products/Commands
Core/CleanArc.Application/Features/Products/Queries
Core/CleanArc.Application/Features/Products/DTOs
3. Define the Command and Handler
Step 3.1: Add the AddProductCommand class in Commands:

csharp
Copy code
public record AddProductCommand(string Name, decimal Price, string Category) 
    : IRequest<ProductDto>, IValidatableModel<AddProductCommand>
{
    public IValidator<AddProductCommand> ValidateApplicationModel(ApplicationBaseValidationModelProvider<AddProductCommand> validator)
    {
        validator.RuleFor(c => c.Name).NotEmpty().WithMessage("Name is required.");
        validator.RuleFor(c => c.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
        return validator;
    }
};
Step 3.2: Add the AddProductCommandHandler in the same folder:

csharp
Copy code
internal class AddProductCommandHandler : IRequestHandler<AddProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public AddProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product { Name = request.Name, Price = request.Price, Category = request.Category };
        await _productRepository.AddProductAsync(product);
        
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category
        };
    }
}
4. Update the Domain Layer
Step 4.1: Add the Product entity in Core/CleanArc.Domain/Entities:

csharp
Copy code
public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
}
5. Update the Infrastructure Layer
Step 5.1: Add a Repository Interface

Add a new interface IProductRepository in Core/CleanArc.Application/Contracts/Persistence:
csharp
Copy code
public interface IProductRepository
{
    Task AddProductAsync(Product product);
    Task<Product> GetProductByIdAsync(int id);
}
Step 5.2: Add a Repository Implementation

Add ProductRepository in Infrastructure/CleanArc.Infrastructure.Persistence/Repositories:
csharp
Copy code
internal class ProductRepository : BaseAsyncRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext dbContext) : base(dbContext) { }

    public async Task AddProductAsync(Product product)
    {
        await base.AddAsync(product);
    }

    public async Task<Product> GetProductByIdAsync(int id)
    {
        return await base.TableNoTracking.FirstOrDefaultAsync(p => p.Id == id);
    }
}
Step 5.3: Register the Repository in ServiceCollectionExtensions.cs:

csharp
Copy code
services.AddScoped<IProductRepository, ProductRepository>();
Step 5.4: Update the Database Context

Add DbSet<Product> to ApplicationDbContext:
csharp
Copy code
public DbSet<Product> Products { get; set; }
6. Update the API Layer
Step 6.1: Add a Controller

Create ProductController in API/CleanArc.Web.Api/Controllers/V1:
csharp
Copy code
[ApiVersion("1")]
[ApiController]
[Route("api/v{version:apiVersion}/products")]
public class ProductController : ControllerBase
{
    private readonly ISender _sender;

    public ProductController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] AddProductCommand command)
    {
        var result = await _sender.Send(command);
        return Ok(result);
    }
}
Step 6.2: Add Swagger Documentation

Update SwaggerConfigurationExtensions.cs to include the new endpoint.
7. Update the Database
Step 7.1: Add a Migration

Run the following command:
bash
Copy code
dotnet ef migrations add AddProductsTable -p Infrastructure/CleanArc.Infrastructure.Persistence -s API/CleanArc.Web.Api
Step 7.2: Apply the Migration

Run:
bash
Copy code
dotnet ef database update -p Infrastructure/CleanArc.Infrastructure.Persistence -s API/CleanArc.Web.Api
8. Add Error Handling
Ensure validation and error-handling middleware is active:
csharp
Copy code
app.UseMiddleware<ExceptionHandler>();
9. Write Tests
Step 9.1: Unit Tests

Add tests for AddProductCommandHandler in Tests/BaseSetup:
csharp
Copy code
[Fact]
public async Task AddProduct_ShouldReturnProductDto()
{
    var repositoryMock = new Mock<IProductRepository>();
    var handler = new AddProductCommandHandler(repositoryMock.Object);

    var command = new AddProductCommand("Test Product", 100.0M, "Category");

    var result = await handler.Handle(command, CancellationToken.None);

    Assert.NotNull(result);
    Assert.Equal("Test Product", result.Name);
}
Step 9.2: Integration Tests

Add tests for the endpoint in Tests/Infrastructure.

**Step 8.2**: Write Integration Tests  
Setup test environment in `TestApplicationDbContext.cs`.

---



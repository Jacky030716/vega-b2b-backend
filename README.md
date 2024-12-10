# MemowordCleanArc


### Expanded and Example-Based Step-by-Step Guidelines for Creating One API

Using examples extracted from your project, here's a tailored guide for creating a new API endpoint:

---

#### **1. Define the Requirements**
**Example: Create an API for adding a "Product".**
- **HTTP Method**: `POST`
- **Endpoint Route**: `/api/v1/products`
- **Input DTO**:
  ```json
  {
      "name": "Product Name",
      "price": 100.00,
      "category": "Category Name"
  }
  ```
- **Output DTO**:
  ```json
  {
      "id": 1,
      "name": "Product Name",
      "price": 100.00,
      "category": "Category Name"
  }
  ```

---

#### **2. Update the Core Layer**

**Step 2.1**: Define the Command  
Example from `AddAdminCommand.cs`:
```csharp
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
```

**Step 2.2**: Implement the Handler  
Example from `AddAdminCommand.Handler.cs`:
```csharp
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
```

**Step 2.3**: Add DTOs  
Create `ProductDto.cs`:
```csharp
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
}
```

---

#### **3. Update the Domain Layer**

**Step 3.1**: Define the Product Entity  
Example from `Order.cs`:
```csharp
public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
}
```

---

#### **4. Update the Infrastructure Layer**

**Step 4.1**: Define the Repository Interface  
Example from `IOrderRepository.cs`:
```csharp
public interface IProductRepository
{
    Task AddProductAsync(Product product);
    Task<Product> GetProductByIdAsync(int id);
}
```

**Step 4.2**: Implement the Repository  
Example from `OrderRepository.cs`:
```csharp
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
```

---

#### **5. Update the API Layer**

**Step 5.1**: Create a Controller  
Example from `AdminManagerController.cs`:
```csharp
[ApiVersion("1")]
[ApiController]
[Route("api/v{version:apiVersion}/products")]
public class ProductController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] AddProductCommand command)
    {
        var result = await sender.Send(command);
        return Ok(result);
    }
}
```

---

#### **6. Add Error Handling**
**Example**: Use middleware or filters for error handling.  
From `WebFramework/Middlewares/ExceptionHandler.cs`:
```csharp
app.UseMiddleware<ExceptionHandler>();
```

---

#### **7. Document the API**
Example from Swagger setup:
```csharp
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Product API", Version = "v1" });
});
```

---

#### **8. Write Tests**

**Step 8.1**: Write Unit Tests  
Example from `AddAdminCommandTests`:
```csharp
[Fact]
public async Task AddProductCommandHandler_ShouldReturnProductDto()
{
    var repositoryMock = new Mock<IProductRepository>();
    var handler = new AddProductCommandHandler(repositoryMock.Object);

    var command = new AddProductCommand("Test Product", 100.0M, "Category");

    var result = await handler.Handle(command, CancellationToken.None);

    Assert.NotNull(result);
    Assert.Equal("Test Product", result.Name);
}
```

**Step 8.2**: Write Integration Tests  
Setup test environment in `TestApplicationDbContext.cs`.

---



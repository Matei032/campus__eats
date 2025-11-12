using CampusEats.Backend.Features.Menu;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Menu;

public class CreateProductTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithProduct()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CreateProduct.Handler(context);
        
        var command = new CreateProduct.Command
        {
            Name = "Test Burger",
            Description = "Delicious test burger",
            Price = 25.50m,
            Category = "Main",
            ImageUrl = "https://example.com/burger.jpg",
            Allergens = new List<string> { "Gluten", "Lactose" },
            DietaryRestrictions = "None",
            IsAvailable = true
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Burger");
        result.Value.Price.Should().Be(25.50m);
        result.Value.Category.Should().Be("Main");
        result.Value.Allergens.Should().HaveCount(2);
        result.Value.Allergens.Should().Contain("Gluten");
        result.Value.Allergens.Should().Contain("Lactose");
        
        // Verify it was saved to database
        var productInDb = await context.Products.FindAsync(result.Value.Id);
        productInDb.Should().NotBeNull();
        productInDb!.Name.Should().Be("Test Burger");
    }

    [Fact]
    public async Task Handle_ValidCommand_ProductIsSavedToDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CreateProduct.Handler(context);
        
        var command = new CreateProduct.Command
        {
            Name = "Pizza Test",
            Description = "Test pizza description",
            Price = 30.00m,
            Category = "Main",
            Allergens = new List<string> { "Gluten" },
            IsAvailable = true
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        context.Products.Should().HaveCount(1);
        var savedProduct = context.Products.First();
        savedProduct.Name.Should().Be("Pizza Test");
        savedProduct.Id.Should().Be(result.Value!.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsCreatedAtTimestamp()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CreateProduct.Handler(context);
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);
        
        var command = new CreateProduct.Command
        {
            Name = "Test Product",
            Description = "Test",
            Price = 10.00m,
            Category = "Snack",
            Allergens = new List<string>(),
            IsAvailable = true
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value!.CreatedAt.Should().BeAfter(beforeCreate);
        result.Value.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }
}
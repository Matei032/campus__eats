using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Menu;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Menu;

public class GetMenuTests
{
    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetMenu.Handler(context);
        var query = new GetMenu.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithProducts_ReturnsAllAvailableProducts()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        
        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Burger",
                Description = "Test burger",
                Price = 25.00m,
                Category = "Main",
                Allergens = new List<string> { "Gluten" },
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Pizza",
                Description = "Test pizza",
                Price = 30.00m,
                Category = "Main",
                Allergens = new List<string> { "Gluten", "Lactose" },
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Salad",
                Description = "Test salad",
                Price = 20.00m,
                Category = "Main",
                Allergens = new List<string>(),
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            }
        };
        
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        
        var handler = new GetMenu.Handler(context);
        var query = new GetMenu.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(p => p.Name == "Burger");
        result.Value.Should().Contain(p => p.Name == "Pizza");
        result.Value.Should().Contain(p => p.Name == "Salad");
    }

    [Fact]
    public async Task Handle_WithUnavailableProducts_ReturnsOnlyAvailableProducts()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        
        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Available Product",
                Description = "Test",
                Price = 10.00m,
                Category = "Snack",
                Allergens = new List<string>(),
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Unavailable Product",
                Description = "Test",
                Price = 15.00m,
                Category = "Snack",
                Allergens = new List<string>(),
                IsAvailable = false,
                CreatedAt = DateTime.UtcNow
            }
        };
        
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        
        var handler = new GetMenu.Handler(context);
        var query = new GetMenu.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value!.First().Name.Should().Be("Available Product");
        result.Value.Should().NotContain(p => p.Name == "Unavailable Product");
    }
}
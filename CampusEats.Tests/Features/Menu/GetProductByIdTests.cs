using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Menu;
using CampusEats.Backend.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Menu;

public class GetProductByIdTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidProductId_ReturnsProduct()
    {
        // Arrange
        var context = GetInMemoryContext();
        var productId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "Margherita Pizza",
            Description = "Classic pizza",
            Price = 25m,
            Category = "Main",
            IsAvailable = true,
            ImageUrl = "http://example.com/pizza.jpg",
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new GetProductById.Handler(context);
        var query = new GetProductById.Query(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(productId);
        result.Value.Name.Should().Be("Margherita Pizza");
        result.Value.Price.Should().Be(25m);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryContext();
        var handler = new GetProductById.Handler(context);
        var query = new GetProductById.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ReturnsProductWithAllDetails()
    {
        // Arrange
        var context = GetInMemoryContext();
        var productId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "Vegan Burger",
            Description = "Plant-based burger",
            Price = 18m,
            Category = "Main",
            ImageUrl = "http://example.com/burger.jpg",
            IsAvailable = true,
            Allergens = new List<string> { "Soy", "Gluten" },
            DietaryRestrictions = "Vegan",
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new GetProductById.Handler(context);
        var query = new GetProductById.Query(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Vegan Burger");
        result.Value.Price.Should().Be(18m);
        result.Value.Allergens.Should().Contain("Soy");
        result.Value.Allergens.Should().Contain("Gluten");
    }
}
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Menu;
using CampusEats.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests.Features.Menu;

public class DeleteProductTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var productId = Guid.NewGuid();
        
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Test",
            Price = 10.00m,
            Category = "Snack",
            Allergens = new List<string>(),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        
        var handler = new DeleteProduct.Handler(context);
        var command = new DeleteProduct.Command(productId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistentProduct_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new DeleteProduct.Handler(context);
        var nonExistentId = Guid.NewGuid();
        var command = new DeleteProduct.Command(nonExistentId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(nonExistentId.ToString());
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesProductFromDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var productId = Guid.NewGuid();
        
        var product = new Product
        {
            Id = productId,
            Name = "To Delete",
            Description = "Test",
            Price = 10.00m,
            Category = "Snack",
            Allergens = new List<string>(),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        
        var handler = new DeleteProduct.Handler(context);
        var command = new DeleteProduct.Command(productId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var deletedProduct = await context.Products.FindAsync(productId);
        deletedProduct.Should().BeNull();
        context.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProductWithOrders_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Create product
        var product = new Product
        {
            Id = productId,
            Name = "Product with Order",
            Description = "Test",
            Price = 10.00m,
            Category = "Main",
            Allergens = new List<string>(),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        
        // Create user
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            FullName = "Test User",
            PhoneNumber = "1234567890",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        
        // Create order
        var order = new Order
        {
            Id = orderId,
            OrderNumber = "TEST-001",
            UserId = userId,
            TotalAmount = 10.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.Orders.Add(order);
        
        // Create order item linking product to order
        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = 1,
            UnitPrice = 10.00m,
            Subtotal = 10.00m
        };
        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();
        
        var handler = new DeleteProduct.Handler(context);
        var command = new DeleteProduct.Command(productId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot delete product");
        result.Error.Should().Contain("ordered");
        
        // Product should still exist
        var productStillExists = await context.Products.FindAsync(productId);
        productStillExists.Should().NotBeNull();
    }
}
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Orders;
using CampusEats.Backend.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Orders;

public class GetOrderByIdTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidOrderId_ReturnsOrder()
    {
        // Arrange
        var context = GetInMemoryContext();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Customer",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = productId,
            Name = "Pizza",
            Description = "Delicious",
            Price = 25m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            UserId = userId,
            TotalAmount = 50m,
            Status = "Completed",
            PaymentStatus = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = 2,
            UnitPrice = 25m,
            Subtotal = 50m
        };

        context.Users.Add(user);
        context.Products.Add(product);
        context.Orders.Add(order);
        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        var handler = new GetOrderById.Handler(context);
        var query = new GetOrderById.Query { OrderId = orderId, UserId = userId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(orderId);
        result.Value.TotalAmount.Should().Be(50m);
        result.Value.Status.Should().Be("Completed");
        result.Value.OrderItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryContext();
        var handler = new GetOrderById.Handler(context);
        var nonExistentOrderId = Guid.NewGuid();
        var query = new GetOrderById.Query { OrderId = nonExistentOrderId, UserId = Guid.NewGuid() };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_IncludesOrderItemsAndProducts()
    {
        // Arrange
        var context = GetInMemoryContext();
        var userId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Customer",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product1 = new Product
        {
            Id = product1Id,
            Name = "Pizza",
            Description = "Tasty",
            Price = 25m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var product2 = new Product
        {
            Id = product2Id,
            Name = "Salad",
            Description = "Fresh",
            Price = 15m,
            Category = "Side",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            UserId = userId,
            TotalAmount = 65m,
            Status = "Completed",
            PaymentStatus = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        var orderItem1 = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = product1Id,
            Quantity = 2,
            UnitPrice = 25m,
            Subtotal = 50m
        };

        var orderItem2 = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = product2Id,
            Quantity = 1,
            UnitPrice = 15m,
            Subtotal = 15m
        };

        context.Users.Add(user);
        context.Products.AddRange(product1, product2);
        context.Orders.Add(order);
        context.OrderItems.AddRange(orderItem1, orderItem2);
        await context.SaveChangesAsync();

        var handler = new GetOrderById.Handler(context);
        var query = new GetOrderById.Query { OrderId = orderId, UserId = userId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderItems.Should().HaveCount(2);
        result.Value.OrderItems.Should().Contain(i => i.ProductName == "Pizza");
        result.Value.OrderItems.Should().Contain(i => i.ProductName == "Salad");
    }
}
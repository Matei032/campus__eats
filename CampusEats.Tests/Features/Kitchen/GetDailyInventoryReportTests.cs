using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Kitchen;
using CampusEats.Backend.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Kitchen;

public class GetDailyInventoryReportTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_NoOrdersToday_ReturnsEmptyReport()
    {
        // Arrange
        var context = GetInMemoryContext();
        var today = DateTime.UtcNow.Date;

        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = today };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalRevenue.Should().Be(0);
        result.Value.TotalOrdersProcessed.Should().Be(0);
        result.Value.TotalItemsSold.Should().Be(0);
        result.Value.InventoryItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithCompletedOrdersToday_ReturnsReport()
    {
        // Arrange
        var context = GetInMemoryContext();
        var today = DateTime.UtcNow.Date;
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
            Name = "Burger",
            Description = "Tasty",
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
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.SpecifyKind(today.AddHours(12), DateTimeKind.Utc)
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

        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = today };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalRevenue.Should().Be(50m);
        result.Value.TotalOrdersProcessed.Should().Be(1);
        result.Value.TotalItemsSold.Should().Be(2);
        result.Value.InventoryItems.Should().HaveCount(1);
        result.Value.InventoryItems.First().ProductName.Should().Be("Burger");
        result.Value.InventoryItems.First().QuantitySold.Should().Be(2);
    }
}
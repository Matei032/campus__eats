using CampusEats.Backend.Features.Kitchen;
using CampusEats.Backend.Persistence;
using CampusEats.Backend.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Backend.Tests.Features.Kitchen;

public class GetPendingOrdersTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_Return_Pending_And_Preparing_Orders()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 10.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var pendingOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Pending",
            PaymentStatus = "Paid",
            TotalAmount = 10.00m,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };

        var preparingOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-002", 
            UserId = user.Id,
            Status = "Preparing",
            PaymentStatus = "Paid",
            TotalAmount = 15.00m,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var completedOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-003",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid", 
            TotalAmount = 20.00m,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddRangeAsync(pendingOrder, preparingOrder, completedOrder);
        await context.SaveChangesAsync();

        var handler = new GetPendingOrders.Handler(context);
        var query = new GetPendingOrders.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(o => o.Status == "Pending");
        result.Value.Should().Contain(o => o.Status == "Preparing");
        result.Value.Should().NotContain(o => o.Status == "Completed");
        
        // Should be ordered by creation time (oldest first - FIFO)
        result.Value!.First().OrderNumber.Should().Be("ORD-001");
        result.Value.Last().OrderNumber.Should().Be("ORD-002");
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_When_No_Pending_Orders()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var handler = new GetPendingOrders.Handler(context);
        var query = new GetPendingOrders.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
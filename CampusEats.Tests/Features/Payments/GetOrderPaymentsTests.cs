using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Payments;
using CampusEats.Backend.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Payments;

public class GetOrderPaymentsTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsPaymentsForOrder()
    {
        // Arrange
        var context = GetInMemoryContext();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "user@test. com",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Customer",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            UserId = userId,
            TotalAmount = 100m,
            Status = "Completed",
            PaymentStatus = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        var payment1 = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 50m,
            Method = PaymentMethod.Card,
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            PaidAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var payment2 = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 50m,
            Method = PaymentMethod.LoyaltyPoints,
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            PaidAt = DateTime.UtcNow.AddMinutes(-5)
        };

        context.Users.Add(user);
        context.Orders.Add(order);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var handler = new GetOrderPayments.Handler(context);
        var query = new GetOrderPayments.Query(orderId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Amount == 50m && p.Method == PaymentMethod.Card);
        result.Should().Contain(p => p.Amount == 50m && p.Method == PaymentMethod.LoyaltyPoints);
    }

    [Fact]
    public async Task Handle_OrderWithNoPayments_ReturnsEmptyList()
    {
        // Arrange
        var context = GetInMemoryContext();
        var userId = Guid.NewGuid();
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

        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            UserId = userId,
            TotalAmount = 100m,
            Status = "Pending",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var handler = new GetOrderPayments.Handler(context);
        var query = new GetOrderPayments.Query(orderId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsPaymentsOrderedByCreatedAtDescending()
    {
        // Arrange
        var context = GetInMemoryContext();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "user@test. com",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Customer",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            UserId = userId,
            TotalAmount = 150m,
            Status = "Completed",
            PaymentStatus = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        var payment1 = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 50m,
            Method = PaymentMethod.Card,
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            PaidAt = DateTime.UtcNow.AddMinutes(-20)
        };

        var payment2 = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 100m,
            Method = PaymentMethod.Card,
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            PaidAt = DateTime.UtcNow.AddMinutes(-10)
        };

        context.Users.Add(user);
        context.Orders.Add(order);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var handler = new GetOrderPayments.Handler(context);
        var query = new GetOrderPayments.Query(orderId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.First().Amount.Should().Be(100m); // Most recent first
        result.Last().Amount.Should().Be(50m);
    }
}
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Payments;
using CampusEats.Backend.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Payments;

public class GetUserPaymentsTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsUserPaymentsOnly()
    {
        // Arrange
        var context = GetInMemoryContext();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var order1Id = Guid.NewGuid();
        var order2Id = Guid.NewGuid();

        var user1 = new User
        {
            Id = user1Id,
            Email = "user1@test.com",
            PasswordHash = "hash",
            FullName = "User One",
            Role = "Customer",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = user2Id,
            Email = "user2@test.com",
            PasswordHash = "hash",
            FullName = "User Two",
            Role = "Customer",
            PhoneNumber = "+40712345679",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var order1 = new Order
        {
            Id = order1Id,
            OrderNumber = "ORD-001",
            UserId = user1Id,
            TotalAmount = 50m,
            Status = "Completed",
            PaymentStatus = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        var order2 = new Order
        {
            Id = order2Id,
            OrderNumber = "ORD-002",
            UserId = user2Id,
            TotalAmount = 75m,
            Status = "Completed",
            PaymentStatus = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        var payment1 = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order1Id,
            Amount = 50m,
            Method = PaymentMethod.Card,
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        var payment2 = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order2Id,
            Amount = 75m,
            Method = PaymentMethod.Cash,
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        context.Users.AddRange(user1, user2);
        context.Orders.AddRange(order1, order2);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var handler = new GetUserPayments.Handler(context);
        var query = new GetUserPayments.Query(user1Id, 1, 10, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Amount.Should().Be(50m);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UserWithNoPayments_ReturnsEmptyList()
    {
        // Arrange
        var context = GetInMemoryContext();
        var userId = Guid.NewGuid();

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

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetUserPayments.Handler(context);
        var query = new GetUserPayments.Query(userId, 1, 10, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_FiltersByStatus()
    {
        // Arrange
        var context = GetInMemoryContext();
        var userId = Guid.NewGuid();
        var order1Id = Guid.NewGuid();
        var order2Id = Guid.NewGuid();

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

        var order1 = new Order
        {
            Id = order1Id,
            OrderNumber = "ORD-001",
            UserId = userId,
            TotalAmount = 50m,
            Status = "Completed",
            PaymentStatus = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        var order2 = new Order
        {
            Id = order2Id,
            OrderNumber = "ORD-002",
            UserId = userId,
            TotalAmount = 75m,
            Status = "Pending",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        var payment1 = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order1Id,
            Amount = 50m,
            Method = PaymentMethod.Card,
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        var payment2 = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order2Id,
            Amount = 75m,
            Method = PaymentMethod.Cash,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Orders.AddRange(order1, order2);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var handler = new GetUserPayments.Handler(context);
        var query = new GetUserPayments.Query(userId, 1, 10, "Completed", null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Status.Should().Be(PaymentStatus.Completed);
    }
}
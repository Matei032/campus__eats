using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Payments;
using CampusEats.Backend.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Payments;

public class GetAllPaymentsTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsAllPayments()
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

        context.Users.Add(user);
        context.Orders.AddRange(order1, order2);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var handler = new GetAllPayments.Handler(context);
        var query = new GetAllPayments.Query(1, 10, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.CurrentPage.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_NoPayments_ReturnsEmptyList()
    {
        // Arrange
        var context = GetInMemoryContext();
        var handler = new GetAllPayments.Handler(context);
        var query = new GetAllPayments.Query(1, 10, null, null);

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

        var handler = new GetAllPayments.Handler(context);
        var query = new GetAllPayments.Query(1, 10, "Completed", null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Status.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public async Task Handle_FiltersByMethod()
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

        context.Users.Add(user);
        context.Orders.AddRange(order1, order2);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var handler = new GetAllPayments.Handler(context);
        var query = new GetAllPayments.Query(1, 10, null, "Card");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Method.Should().Be(PaymentMethod.Card);
    }
}
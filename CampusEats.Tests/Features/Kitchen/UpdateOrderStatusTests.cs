using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Kitchen;
using CampusEats.Backend.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Kitchen;

public class UpdateOrderStatusTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_UpdateToPreparing_Success()
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
            TotalAmount = 50m,
            Status = "Pending",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command { OrderId = orderId, Status = "Preparing" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Preparing");

        var updatedOrder = await context.Orders.FindAsync(orderId);
        updatedOrder!.Status.Should().Be("Preparing");
        updatedOrder.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_UpdateToCompleted_AwardsLoyaltyPoints()
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
            LoyaltyPoints = 0,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            UserId = userId,
            TotalAmount = 100m,
            Status = "Ready",
            PaymentStatus = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command { OrderId = orderId, Status = "Completed" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Completed");
        result.Value.PaymentStatus.Should().Be("Paid");
        result.Value.CompletedAt.Should().NotBeNull();

        var updatedOrder = await context.Orders.FindAsync(orderId);
        updatedOrder!.Status.Should().Be("Completed");
        updatedOrder.CompletedAt.Should().NotBeNull();

        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser!.LoyaltyPoints.Should().Be(10m); // 10% of 100 = 10 points
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryContext();
        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command { OrderId = Guid.NewGuid(), Status = "Preparing" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_UpdateAlreadyCompletedOrder_SucceedsButNoExtraPoints()
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
            LoyaltyPoints = 10m,
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
            CompletedAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command { OrderId = orderId, Status = "Completed" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser!.LoyaltyPoints.Should().Be(10m); // No extra points awarded
    }
}
using CampusEats.Backend.Features.Kitchen;
using CampusEats.Backend.Persistence;
using CampusEats.Backend.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Backend.Tests.Features.Kitchen;

public class UpdateOrderStatusTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Theory]
    [InlineData("Pending", "Preparing")]
    [InlineData("Pending", "Cancelled")]
    [InlineData("Preparing", "Ready")]
    [InlineData("Preparing", "Cancelled")]
    [InlineData("Ready", "Completed")]
    [InlineData("Ready", "Cancelled")]
    public async Task Handle_Should_Allow_Valid_Status_Transitions(string fromStatus, string toStatus)
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

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = fromStatus,
            PaymentStatus = "Pending",
            TotalAmount = 25.00m,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        await context.Users.AddAsync(user);
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command
        {
            OrderId = order.Id,
            Status = toStatus
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(toStatus);
        result.Value.UpdatedAt.Should().NotBeNull();
        
        if (toStatus == "Completed")
        {
            result.Value.PaymentStatus.Should().Be("Paid");
            result.Value.CompletedAt.Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData("Pending", "Completed")]
    [InlineData("Preparing", "Pending")]
    [InlineData("Ready", "Preparing")]
    [InlineData("Completed", "Ready")]
    [InlineData("Cancelled", "Pending")]
    public async Task Handle_Should_Reject_Invalid_Status_Transitions(string fromStatus, string toStatus)
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

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = fromStatus,
            PaymentStatus = "Pending",
            TotalAmount = 25.00m,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        await context.Users.AddAsync(user);
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command
        {
            OrderId = order.Id,
            Status = toStatus
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid status transition");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command
        {
            OrderId = Guid.NewGuid(),
            Status = "Preparing"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Order not found");
    }
}
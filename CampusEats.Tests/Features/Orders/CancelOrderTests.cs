using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Orders;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Orders;

public class CancelOrderTests
{
    [Fact]
    public async Task Handle_ValidCommand_CancelsOrder()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CancelOrder.Handler(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Test",
            Price = 25.00m,
            Category = "Main",
            Allergens = new List<string>(),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-TEST-001",
            UserId = user.Id,
            TotalAmount = 25.00m,
            Status = "Pending",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 25.00m,
            Subtotal = 25.00m
        };

        context.Users.Add(user);
        context.Products.Add(product);
        context.Orders.Add(order);
        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        var command = new CancelOrder.Command
        {
            OrderId = order.Id,
            UserId = user.Id,
            CancellationReason = "Changed my mind"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Cancelled");
        result.Value.PaymentStatus.Should().Be("Refunded");
        result.Value.Notes.Should().Contain("Changed my mind");
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CancelOrder.Handler(context);

        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@campus.ro", PasswordHash = "hash", FullName = "User 1", Role = "Student", PhoneNumber = "+40712345678", IsActive = true, CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@campus.ro", PasswordHash = "hash", FullName = "User 2", Role = "Student", PhoneNumber = "+40723456789", IsActive = true, CreatedAt = DateTime.UtcNow };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-TEST-001",
            UserId = user1.Id, // Owned by user1
            TotalAmount = 25.00m,
            Status = "Pending",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(user1, user2);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var command = new CancelOrder.Command
        {
            OrderId = order.Id,
            UserId = user2.Id // user2 trying to cancel user1's order
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not authorized"));
    }

    [Fact]
    public async Task Handle_CompletedOrder_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CancelOrder.Handler(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-TEST-001",
            UserId = user.Id,
            TotalAmount = 25.00m,
            Status = "Completed", // Already completed
            PaymentStatus = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var command = new CancelOrder.Command
        {
            OrderId = order.Id,
            UserId = user.Id
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Cannot cancel completed order"));
    }

    [Fact]
    public async Task Handle_AlreadyCancelledOrder_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CancelOrder.Handler(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-TEST-001",
            UserId = user.Id,
            TotalAmount = 25.00m,
            Status = "Cancelled", // Already cancelled
            PaymentStatus = "Refunded",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var command = new CancelOrder.Command
        {
            OrderId = order.Id,
            UserId = user.Id
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already cancelled"));
    }
}
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Payments;
using CampusEats.Backend.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Xunit;

namespace CampusEats.Tests.Features.Payments;

public class RefundPaymentTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryContext();
        var stripeClient = new StripeClient("sk_test_dummy"); // Real client cu dummy key
        var handler = new RefundPayment.Handler(context, stripeClient);
        var command = new RefundPayment.Command(Guid.NewGuid(), "Test reason");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Payment not found");
    }

    [Fact]
    public async Task Handle_PaymentNotCompleted_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryContext();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

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
            TotalAmount = 50m,
            Status = "Pending",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            Amount = 50m,
            Method = PaymentMethod.Card,
            Status = PaymentStatus.Pending, // NOT Completed! 
            StripePaymentIntentId = "pi_test",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Orders.Add(order);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        var stripeClient = new StripeClient("sk_test_dummy");
        var handler = new RefundPayment.Handler(context, stripeClient);
        var command = new RefundPayment.Command(paymentId, "Test");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cannot refund payment that is not completed");
    }

    [Fact]
    public async Task Handle_PaymentMethodNotCard_ReturnsFailure()
    {
        // Arrange
        var context = GetInMemoryContext();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

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
            TotalAmount = 50m,
            Status = "Completed",
            PaymentStatus = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            Amount = 50m,
            Method = PaymentMethod.Cash, // NOT Card!
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Orders.Add(order);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        var stripeClient = new StripeClient("sk_test_dummy");
        var handler = new RefundPayment.Handler(context, stripeClient);
        var command = new RefundPayment.Command(paymentId, "Test");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Only Stripe card payments can be refunded");
    }
}
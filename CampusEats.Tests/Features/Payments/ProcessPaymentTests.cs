using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Payments;
using CampusEats.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Stripe;

namespace CampusEats.Tests.Features.Payments;

public class ProcessPaymentTests
{
    [Fact]
    public async Task Handle_CashPayment_Success()
    {
        var context = TestDbContextFactory.CreateInMemoryContext();

        var user = new User { Id = Guid.NewGuid(), LoyaltyPoints = 100 };
        var order = new Order { Id = Guid.NewGuid(), TotalAmount = 50, PaymentStatus = "Unpaid", UserId = user.Id, User = user };
        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var stripeClient = new StripeClient("sk_test_dummy"); // Stripe mock, nu se folosește la Cash

        var handler = new ProcessPayment.Handler(context, stripeClient);

        var command = new ProcessPayment.Command { OrderId = order.Id, PaymentMethod = "Cash", Amount = 50 };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Method.Should().Be(PaymentMethod.Cash);
        result.Value.Amount.Should().Be(50);
        result.Value.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Handle_Loyalty_Success()
    {
        var context = TestDbContextFactory.CreateInMemoryContext();

        var user = new User { Id = Guid.NewGuid(), LoyaltyPoints = 300 };
        var order = new Order { Id = Guid.NewGuid(), TotalAmount = 100, PaymentStatus = "Unpaid", UserId = user.Id, User = user };
        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var stripeClient = new StripeClient("sk_test_dummy");

        var handler = new ProcessPayment.Handler(context, stripeClient);

        var command = new ProcessPayment.Command { OrderId = order.Id, PaymentMethod = "LoyaltyPoints", Amount = 100, LoyaltyPointsUsed = 100 };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(PaymentStatus.Completed);
        result.Value!.LoyaltyPointsUsed.Should().Be(100);
        user.LoyaltyPoints.Should().Be(200);
    }

    [Fact]
    public async Task Handle_Loyalty_Failure_InsufficientPoints()
    {
        var context = TestDbContextFactory.CreateInMemoryContext();

        var user = new User { Id = Guid.NewGuid(), LoyaltyPoints = 20 };
        var order = new Order { Id = Guid.NewGuid(), TotalAmount = 50, PaymentStatus = "Unpaid", UserId = user.Id, User = user };
        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var stripeClient = new StripeClient("sk_test_dummy");

        var handler = new ProcessPayment.Handler(context, stripeClient);

        var command = new ProcessPayment.Command { OrderId = order.Id, PaymentMethod = "LoyaltyPoints", Amount = 50, LoyaltyPointsUsed = 30 };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Insufficient loyalty points"));
    }

    [Fact]
    public async Task Handle_AlreadyPaid_Failure()
    {
        var context = TestDbContextFactory.CreateInMemoryContext();

        var user = new User { Id = Guid.NewGuid(), LoyaltyPoints = 200 };
        var order = new Order { Id = Guid.NewGuid(), TotalAmount = 100, PaymentStatus = "Paid", UserId = user.Id, User = user };
        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var stripeClient = new StripeClient("sk_test_dummy");

        var handler = new ProcessPayment.Handler(context, stripeClient);

        var command = new ProcessPayment.Command { OrderId = order.Id, PaymentMethod = "Cash", Amount = 100 };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already paid"));
    }

    [Fact]
    public async Task Handle_WrongAmount_Failure()
    {
        var context = TestDbContextFactory.CreateInMemoryContext();

        var user = new User { Id = Guid.NewGuid(), LoyaltyPoints = 100 };
        var order = new Order { Id = Guid.NewGuid(), TotalAmount = 200, PaymentStatus = "Unpaid", UserId = user.Id, User = user };
        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var stripeClient = new StripeClient("sk_test_dummy");

        var handler = new ProcessPayment.Handler(context, stripeClient);

        var command = new ProcessPayment.Command { OrderId = order.Id, PaymentMethod = "Cash", Amount = 150 };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("doesn't match"));
    }

    [Fact]
    public async Task Handle_Card_Success()
    {
        var context = TestDbContextFactory.CreateInMemoryContext();

        var user = new User { Id = Guid.NewGuid(), LoyaltyPoints = 100 };
        var order = new Order { Id = Guid.NewGuid(), TotalAmount = 100, PaymentStatus = "Unpaid", UserId = user.Id, User = user };
        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var stripeClient = new StripeClient("sk_test_dummy");

        var paymentIntentServiceMock = new Mock<PaymentIntentService>(stripeClient);
        paymentIntentServiceMock.Setup(s =>
                s.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentIntent { Id = "pi_test_1", ClientSecret = "test_client_secret" });

        // REF: Modifica handlerul pentru a accepta IPaymentIntentService injectabil pentru testabilitate completă!

        var handler = new ProcessPayment.Handler(context, stripeClient); // momentan, StripeClient

        var command = new ProcessPayment.Command { OrderId = order.Id, PaymentMethod = "Card", Amount = 100 };

        // Atenție: handlerul curent creează serviciu direct, nu poate fi mocat.  
        // Pentru test real, e util să refactorizezi handlerul să accepte injectabil IPaymentIntentService pentru test/mock.

        // result = await handler.Handle(command, CancellationToken.None);
        // result.IsSuccess.Should().BeTrue();
        // result.Value!.StripePaymentIntentId.Should().Be("pi_test_1");
        // result.Value!.StripeClientSecret.Should().Be("test_client_secret");
    }
}
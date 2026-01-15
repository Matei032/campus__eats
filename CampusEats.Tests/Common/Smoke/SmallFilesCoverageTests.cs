using CampusEats.Backend.Common.Config;
using CampusEats.Backend.Common.Exceptions;
using FluentAssertions;

namespace CampusEats.Tests.Common.Smoke;

public class SmallFilesCoverageTests
{
    [Fact]
    public void NotFoundException_ShouldSetMessage()
    {
        var ex = new NotFoundException("Order", 123);

        ex.Message.Should().Contain("Order");
        ex.Message.Should().Contain("123");
    }

    [Fact]
    public void StripeSettings_ShouldAllowSetGet()
    {
        var s = new StripeSettings
        {
            SecretKey = "sk_test_123",
            PublicKey = "pk_test_123",
            WebhookSecret = "whsec_123"
        };

        s.SecretKey.Should().Be("sk_test_123");
        s.PublicKey.Should().Be("pk_test_123");
        s.WebhookSecret.Should().Be("whsec_123");
    }
}
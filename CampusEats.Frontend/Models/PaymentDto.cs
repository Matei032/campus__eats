namespace CampusEats.Frontend.Models;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    
    // Stripe specific
    public string? StripePaymentIntentId { get; set; }
    public string? StripeSessionId { get; set; }
    public string? StripeClientSecret { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    
    // Loyalty
    public int? LoyaltyPointsUsed { get; set; }
}

public class ProcessPaymentRequest
{
    public Guid OrderId { get; set; }
    public string PaymentMethod { get; set; } = "Card";
    public decimal Amount { get; set; }
    public int? LoyaltyPointsUsed { get; set; }
}
namespace CampusEats.Backend.Domain;

public class Payment
{
    public Guid Id { get; set; }
    
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; }
    
    public string? StripePaymentIntentId { get; set; }
    public string? StripeSessionId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}

public enum PaymentMethod
{
    Card = 0,
    Cash = 1,
    LoyaltyPoints = 2,
    Mixed = 3
}
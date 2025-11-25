using System.Text.Json.Serialization;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentStatus Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentMethod Method { get; set; }

    public string? StripePaymentIntentId { get; set; }
    public string? StripeSessionId { get; set; }
    
    public string? StripeClientSecret { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public int? LoyaltyPointsUsed { get; set; }
}
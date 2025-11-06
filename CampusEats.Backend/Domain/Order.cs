namespace CampusEats.Backend.Domain;

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    
    // User Reference
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Order Details
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    // Kitchen Tracking
    public DateTime? PreparedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Timestamps - REMOVED = DateTime.UtcNow
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    
    // Payment Reference
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    
    // Navigation Properties
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
}

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Preparing = 2,
    Ready = 3,
    Completed = 4,
    Cancelled = 5
}
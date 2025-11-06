namespace CampusEats.Backend.Domain;

public class LoyaltyTransaction
{
    public Guid Id { get; set; }
    
    // User Reference
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Transaction Details
    public int PointsChange { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    
    // Optional Order Reference
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    
    // Timestamp - REMOVED = DateTime.UtcNow
    public DateTime CreatedAt { get; set; }
}

public enum LoyaltyTransactionType
{
    Earned = 0,
    Redeemed = 1,
    Expired = 2,
    Adjusted = 3
}
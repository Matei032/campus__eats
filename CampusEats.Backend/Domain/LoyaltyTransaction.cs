namespace CampusEats.Backend.Domain;

public class LoyaltyTransaction
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int PointsChange { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum LoyaltyTransactionType
{
    Earned = 0,
    Redeemed = 1,
    Expired = 2,
    Adjusted = 3
}
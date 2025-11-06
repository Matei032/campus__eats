namespace CampusEats.Backend.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    
    // Loyalty Program
    public int LoyaltyPoints { get; set; } = 0;
    
    // Timestamps - REMOVED = DateTime.UtcNow
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
}
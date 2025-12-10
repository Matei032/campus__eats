namespace CampusEats.Backend.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public decimal LoyaltyPoints { get; set; } = 0;
    public bool IsActive { get; set; } = true; 
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
}
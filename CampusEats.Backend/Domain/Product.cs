namespace CampusEats.Backend.Domain;
using System.ComponentModel.DataAnnotations;

public class Product
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    
    public string? ImageUrl { get; set; }
    public List<string> Allergens { get; set; } = new();
    public string? DietaryRestrictions { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    // Timestamps - REMOVED = DateTime.UtcNow
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
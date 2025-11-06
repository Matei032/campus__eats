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
}
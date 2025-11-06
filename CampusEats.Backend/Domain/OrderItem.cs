namespace CampusEats.Backend.Domain;

public class OrderItem
{
    public Guid Id { get; set; }
    
    // Order Reference
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    // Product Reference
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    // Item Details
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; } // Price at time of order (frozen)
    public decimal Subtotal { get; set; }  // Quantity * UnitPrice
    
    public string? SpecialInstructions { get; set; } // "No onions", "Extra sauce", etc.
}
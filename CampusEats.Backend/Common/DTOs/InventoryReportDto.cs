namespace CampusEats.Backend.Common.DTOs;

// DTO for inventory report
public record InventoryReportDto 
{ 
    public DateTime ReportDate { get; init; } 
    public List<InventoryItemDto> InventoryItems { get; init; } = new(); 
    public decimal TotalRevenue { get; init; } 
    public int TotalOrdersProcessed { get; init; } 
    public int TotalItemsSold { get; init; }
    }

public record InventoryItemDto 
{ 
    public Guid ProductId { get; init; } 
    public string ProductName { get; init; } = string.Empty; 
    public string Category { get; init; } = string.Empty; 
    public int QuantitySold { get; init; } 
    public decimal Revenue { get; init; } 
    public int OrderCount { get; init; } 
    public decimal AverageOrderValue { get; init; }
}
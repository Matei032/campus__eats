namespace CampusEats.Frontend.Models;

public record InventoryReportDto
{
    public DateTime? ReportDate { get; init; }
    public int TotalOrdersProcessed { get; init; }
    public int TotalItemsSold { get; init; }
    public List<InventoryItemDto> InventoryItems { get; init; } = new();
}

public record InventoryItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int QuantitySold { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }
}
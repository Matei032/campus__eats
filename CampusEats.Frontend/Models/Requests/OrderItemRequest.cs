namespace CampusEats.Frontend.Models.Requests;

// ctor (Guid productId, int quantity, string? specialInstructions) – cerut de Cart.razor
public class OrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? SpecialInstructions { get; set; }

    public OrderItemRequest() { }

    public OrderItemRequest(Guid productId, int quantity, string? specialInstructions)
    {
        ProductId = productId;
        Quantity = quantity;
        SpecialInstructions = specialInstructions;
    }
}
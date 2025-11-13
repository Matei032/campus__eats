namespace CampusEats.Frontend.Models.Requests;

public class CreateOrderCommand
{
	public Guid UserId { get; set; }
	public string? Notes { get; set; }
	public List<OrderItemRequest> Items { get; set; } = new();
	public string? PaymentMethod { get; set; }

	public CreateOrderCommand() { }

	// 1) userId, notes, items, paymentMethod
	public CreateOrderCommand(Guid userId, string? notes, List<OrderItemRequest> items, string? paymentMethod)
	{
		UserId = userId;
		Notes = notes;
		Items = items;
		PaymentMethod = paymentMethod;
	}

	// 2) userId, items, notes, paymentMethod
	public CreateOrderCommand(Guid userId, List<OrderItemRequest> items, string? notes, string? paymentMethod)
	{
		UserId = userId;
		Items = items;
		Notes = notes;
		PaymentMethod = paymentMethod;
	}

	// 3) userId, notes, paymentMethod, items  (exact cazul din Cart.razor: arg3 string, arg4 List)
	public CreateOrderCommand(Guid userId, string? notes, string? paymentMethod, List<OrderItemRequest> items)
	{
		UserId = userId;
		Notes = notes;
		PaymentMethod = paymentMethod;
		Items = items;
	}
}
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CampusEats.Frontend.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // ===== MENU =====
    public Task<List<ProductDto>?> GetMenuAsync() =>
        _http.GetFromJsonAsync<List<ProductDto>>("api/menu/");

    public Task<ProductDto?> GetProductAsync(Guid id) =>
        _http.GetFromJsonAsync<ProductDto>($"api/menu/{id}");

    public async Task<ProductDto?> CreateProductAsync(CreateProductCommand cmd)
    {
        var res = await _http.PostAsJsonAsync("api/menu/", cmd, JsonOpts);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ProductDto>(JsonOpts);
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductCommand cmd)
    {
        var res = await _http.PutAsJsonAsync($"api/menu/{id}", cmd, JsonOpts);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ProductDto>(JsonOpts);
    }

    public Task DeleteProductAsync(Guid id) =>
        _http.DeleteAsync($"api/menu/{id}");

    // ===== ORDERS =====
    public async Task<OrderDto?> CreateOrderAsync(CreateOrderCommand cmd)
    {
        var res = await _http.PostAsJsonAsync("api/orders/", cmd, JsonOpts);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<OrderDto>(JsonOpts);
    }

    public Task<List<OrderDto>?> GetUserOrdersAsync(Guid userId) =>
        _http.GetFromJsonAsync<List<OrderDto>>($"api/orders/user/{userId}");

    // Backend cere userId în query pentru GET by id
    public Task<OrderDto?> GetOrderAsync(Guid orderId, Guid userId) =>
        _http.GetFromJsonAsync<OrderDto>($"api/orders/{orderId}?userId={userId}");

    // Backend cere userId + (opțional) cancellationReason
    public Task CancelOrderAsync(Guid orderId, Guid userId, string? cancellationReason = null)
    {
        var reason = cancellationReason is null ? "" : Uri.EscapeDataString(cancellationReason);
        return _http.DeleteAsync($"api/orders/{orderId}?userId={userId}&cancellationReason={reason}");
    }

    // ===== KITCHEN =====
    public Task<List<OrderDto>?> GetPendingOrdersAsync() =>
        _http.GetFromJsonAsync<List<OrderDto>>("api/kitchen/pending");

    public async Task<OrderDto?> UpdateOrderStatusAsync(UpdateOrderStatusCommand cmd)
    {
        var req = new HttpRequestMessage(HttpMethod.Patch, $"api/kitchen/orders/{cmd.OrderId}/status")
        {
            Content = new StringContent(JsonSerializer.Serialize(cmd, JsonOpts), Encoding.UTF8, "application/json")
        };
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<OrderDto>(JsonOpts);
    }

    public Task<InventoryReportDto?> GetDailyInventoryAsync(DateTime? date = null)
    {
        var qs = date is null ? "" : $"?reportDate={date:O}";
        return _http.GetFromJsonAsync<InventoryReportDto>($"api/kitchen/inventory/daily{qs}");
    }
}

// ==== DTOs & Commands (align with backend) ====
public record ProductDto(Guid Id, string Name, string Description, decimal Price, string Category,
    string? ImageUrl, List<string> Allergens, string? DietaryRestrictions, bool IsAvailable,
    DateTime CreatedAt, DateTime? UpdatedAt);

public record OrderItemDto(Guid Id, Guid ProductId, string ProductName, int Quantity,
    decimal UnitPrice, decimal Subtotal, string? SpecialInstructions);

public record OrderDto(Guid Id, string OrderNumber, Guid UserId, decimal TotalAmount, string Status,
    string PaymentStatus, string? PaymentMethod, string? Notes, DateTime CreatedAt,
    DateTime? UpdatedAt, DateTime? CompletedAt, List<OrderItemDto> OrderItems);

public record InventoryItemDto(Guid ProductId, string ProductName, string Category,
    int QuantitySold, decimal Revenue, int OrderCount, decimal AverageOrderValue);

public record InventoryReportDto(DateTime ReportDate, List<InventoryItemDto> InventoryItems,
    decimal TotalRevenue, int TotalOrdersProcessed, int TotalItemsSold);

// commands
public record CreateProductCommand(string Name, string Description, decimal Price, string Category,
    string? ImageUrl, List<string> Allergens, string? DietaryRestrictions, bool IsAvailable = true);
public record UpdateProductCommand(Guid Id, string Name, string Description, decimal Price, string Category,
    string? ImageUrl, List<string> Allergens, string? DietaryRestrictions, bool IsAvailable);
public record CreateOrderCommand(Guid UserId, string? PaymentMethod, string? Notes, List<OrderItemRequest> Items);
public record OrderItemRequest(Guid ProductId, int Quantity, string? SpecialInstructions);
public record UpdateOrderStatusCommand(Guid OrderId, string Status, string? KitchenNotes);
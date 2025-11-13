using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CampusEats.Frontend.Models;
using CampusEats.Frontend.Models.Requests;
using CampusEats.Frontend.Models.Converters;

namespace CampusEats.Frontend.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    // Result simplu (folosit de unele pagini)
    public class Result<T>
    {
        public bool Success { get; set; }
        public T? Value { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // Helper generic – poate fi reutilizat la nevoie
    public async Task<Result<T>> GetAsync<T>(string url)
    {
        var r = new Result<T>();
        try
        {
            var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                r.Success = false;
                r.Errors.Add($"Error {(int)resp.StatusCode} ({resp.StatusCode})");
                return r;
            }
            r.Success = true;
            r.Value = await resp.Content.ReadFromJsonAsync<T>();
            return r;
        }
        catch (Exception ex)
        {
            r.Success = false;
            r.Errors.Add(ex.Message);
            return r;
        }
    }

    // ===== MENU / PRODUCTS =====
    private static readonly JsonSerializerOptions MenuJsonOptions = CreateMenuJsonOptions();
    private static JsonSerializerOptions CreateMenuJsonOptions()
    {
        var o = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        o.Converters.Add(new StringOrArrayConverter());
        return o;
    }

    public async Task<List<ProductDto>> GetMenuAsync()
    {
        var resp = await _http.GetAsync("/api/menu/");
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<List<ProductDto>>(MenuJsonOptions);
        return data ?? new();
    }

    public async Task<ProductDto> GetProductAsync(Guid id)
    {
        var resp = await _http.GetAsync($"/api/menu/{id}");
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<ProductDto>(MenuJsonOptions);
        return data!;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductCommand cmd)
    {
        var resp = await _http.PostAsJsonAsync("/api/menu/", cmd);
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<ProductDto>(MenuJsonOptions);
        return data!;
    }

    public Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductCommand cmd) => UpdateProductAsync(cmd);

    public async Task<ProductDto> UpdateProductAsync(UpdateProductCommand cmd)
    {
        var resp = await _http.PutAsJsonAsync($"/api/menu/{cmd.Id}", cmd);
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<ProductDto>(MenuJsonOptions);
        return data!;
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"/api/menu/{id}");
        resp.EnsureSuccessStatusCode();
    }

    // ===== ORDERS =====
    public async Task<OrderDto> CreateOrderAsync(CreateOrderCommand cmd)
    {
        var resp = await _http.PostAsJsonAsync("/api/orders/", cmd);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(Guid userId)
    {
        var resp = await _http.GetAsync($"/api/orders/user/{userId}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<List<OrderDto>>()) ?? new();
    }

    public async Task<OrderDto> GetOrderAsync(Guid orderId)
    {
        var resp = await _http.GetAsync($"/api/orders/{orderId}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    public Task<OrderDto> GetOrderAsync(Guid orderId, Guid userId) => GetOrderAsync(orderId);

    public async Task<OrderDto> CancelOrderAsync(Guid orderId, string? reason)
    {
        var url = $"/api/orders/{orderId}";
        if (!string.IsNullOrWhiteSpace(reason))
            url += $"?cancellationReason={Uri.EscapeDataString(reason)}";

        var req = new HttpRequestMessage(HttpMethod.Delete, url);
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    public Task<OrderDto> CancelOrderAsync(Guid orderId, Guid userId, string? reason)
        => CancelOrderAsync(orderId, reason);

    // ===== KITCHEN / INVENTORY =====
    public async Task<List<OrderDto>> GetPendingKitchenOrdersAsync()
    {
        var resp = await _http.GetAsync("/api/kitchen/pending");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<List<OrderDto>>()) ?? new();
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, string newStatus)
    {
        var patch = new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/kitchen/orders/{orderId}/status")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { NewStatus = newStatus }),
                Encoding.UTF8,
                "application/json")
        };
        var resp = await _http.SendAsync(patch);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    public async Task<Result<InventoryReportDto>> GetDailyInventoryAsync(DateTime? date)
    {
        var result = new Result<InventoryReportDto>();
        try
        {
            var url = "/api/kitchen/inventory/daily";
            if (date.HasValue) url += $"?reportDate={date.Value:yyyy-MM-dd}";

            var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                result.Success = false;
                result.Errors.Add($"Error {(int)resp.StatusCode} ({resp.StatusCode})");
                return result;
            }
            result.Success = true;
            result.Value = await resp.Content.ReadFromJsonAsync<InventoryReportDto>();
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(ex.Message);
            return result;
        }
    }
}
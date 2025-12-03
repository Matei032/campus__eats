using System.Net.Http.Json;
using CampusEats.Frontend.Models;
using CampusEats.Frontend.Models.Requests;

namespace CampusEats.Frontend.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;

        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ================== MENU / PRODUCTS ==================

        // Folosit în Menu.razor și ProductsAdmin.razor
        public async Task<List<ProductDto>> GetMenuAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<ProductDto>>("api/menu") ?? new();
        }

        // Folosit în EditProduct.razor
        public async Task<ProductDto?> GetProductAsync(Guid id)
        {
            return await _httpClient.GetFromJsonAsync<ProductDto>($"api/menu/{id}");
        }

        public async Task CreateProductAsync(CreateProductCommand command)
        {
            var response = await _httpClient.PostAsJsonAsync("api/menu", command);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Eroare creare produs: {response.StatusCode} - {error}");
            }
        }

        public async Task UpdateProductAsync(Guid id, UpdateProductCommand command)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/menu/{id}", command);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Eroare actualizare produs: {response.StatusCode} - {error}");
            }
        }

        // Folosit în ProductsAdmin.razor
        public async Task DeleteProductAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/menu/{id}");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Nu s-a putut șterge produsul.");
            }
        }

        // ================== ORDERS (CLIENT) ==================

        public async Task<OrderDto?> CreateOrderAsync(CreateOrderCommand command)
        {
             var response = await _httpClient.PostAsJsonAsync("api/orders", command);
             if (response.IsSuccessStatusCode)
             {
                 return await response.Content.ReadFromJsonAsync<OrderDto>();
             }
             return null;
        }

        // Folosit în Orders.razor (istoric comenzi user)
        public async Task<List<OrderDto>> GetUserOrdersAsync(Guid userId)
        {
            return await _httpClient.GetFromJsonAsync<List<OrderDto>>($"api/orders/user/{userId}") ?? new();
        }

        // Folosit în OrderDetails.razor
        public async Task<OrderDto?> GetOrderAsync(Guid orderId)
        {
            try 
            {
                return await _httpClient.GetFromJsonAsync<OrderDto>($"api/orders/{orderId}");
            }
            catch 
            {
                return null;
            }
        }

        // Folosit în OrderDetails.razor
        public async Task CancelOrderAsync(Guid orderId, string reason = "Canceled by user")
        {
            // Endpoint-ul din backend este DELETE /api/orders/{id}?cancellationReason=...
            var url = $"api/orders/{orderId}?cancellationReason={Uri.EscapeDataString(reason)}";
            var response = await _httpClient.DeleteAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Eroare la anularea comenzii.");
            }
        }

        // ================== KITCHEN & INVENTORY (STAFF) ==================

        // Folosit în Kitchen.razor
        public async Task<List<OrderDto>> GetPendingKitchenOrdersAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<OrderDto>>("api/kitchen/pending") ?? new();
        }

        // Folosit în Kitchen.razor pentru a avansa starea (Preparing -> Ready)
        public async Task UpdateOrderStatusAsync(Guid orderId, string newStatus)
        {
            // Backend-ul se așteaptă la un body JSON cu { Status = "..." }
            var payload = new { Status = newStatus }; 
            var response = await _httpClient.PatchAsJsonAsync($"api/kitchen/orders/{orderId}/status", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Nu s-a putut actualiza statusul comenzii.");
            }
        }

        // Folosit în Inventory.razor
        public async Task<InventoryReportDto?> GetDailyInventoryAsync(DateTime? date = null)
        {
            var url = "api/kitchen/inventory/daily";
            if (date.HasValue)
            {
                url += $"?reportDate={date.Value:yyyy-MM-dd}";
            }
            return await _httpClient.GetFromJsonAsync<InventoryReportDto>(url);
        }
        
        // ================== PAYMENTS ==================

        public async Task<PaymentDto?> ProcessPaymentAsync(ProcessPaymentRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/payments/process", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PaymentDto>();
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Payment API Error: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"Payment failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in ProcessPaymentAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PaymentDto>> GetUserPaymentHistoryAsync(Guid userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<PaymentDto>>($"api/payments/user/{userId}/history") 
                       ?? new List<PaymentDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching payment history: {ex.Message}");
                return new List<PaymentDto>();
            }
        }

        public async Task<List<PaymentDto>> GetOrderPaymentsAsync(Guid orderId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<PaymentDto>>($"api/payments/order/{orderId}") 
                       ?? new List<PaymentDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching order payments: {ex.Message}");
                return new List<PaymentDto>();
            }
        }

        // Adaugă această metodă pentru Checkout.razor
        public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<OrderDto>($"api/orders/{orderId}");
            }
            catch
            {
                return null;
            }
        }
    }
}
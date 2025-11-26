using System.Net.Http.Json;
using CampusEats.Frontend.Models.Requests;

namespace CampusEats.Frontend.Services
{
    public class OrderService
    {
        private readonly HttpClient _httpClient;

        public OrderService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> PlaceOrder(CreateOrderCommand command)
        {
            try
            {
                // Endpoint standard pentru CreateOrder
                var response = await _httpClient.PostAsJsonAsync("api/orders", command);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                // Logăm eroarea pentru debugging
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Eroare backend: {error}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare rețea: {ex.Message}");
                return false;
            }
        }
    }
}
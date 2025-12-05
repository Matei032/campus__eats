using System. Net.Http. Json;
using CampusEats.Frontend.Models;

namespace CampusEats.Frontend. Services;

public class LoyaltyService
{
    private readonly HttpClient _httpClient;

    public LoyaltyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Obține informațiile complete de loialitate (puncte curente, total earned, etc.)
    /// </summary>
    public async Task<LoyaltyPointsDto? > GetLoyaltyInfo()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<LoyaltyPointsDto>("api/loyalty/points");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting loyalty info: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Obține doar punctele curente (wrapper peste GetLoyaltyInfo)
    /// </summary>
    public async Task<int> GetPoints()
    {
        try
        {
            var info = await GetLoyaltyInfo();
            return info?.CurrentPoints ?? 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting loyalty points: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Redeem (folosește) puncte de loialitate
    /// Backend: POST /api/loyalty/redeem
    /// </summary>
    public async Task<bool> Redeem(Guid userId, int pointsToRedeem)
    {
        try
        {
            var payload = new { UserId = userId, PointsToRedeem = pointsToRedeem };
            var response = await _httpClient.PostAsJsonAsync("api/loyalty/redeem", payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error redeeming points: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obține istoricul tranzacțiilor de loyalty
    /// </summary>
    public async Task<List<LoyaltyTransactionDto>> GetTransactions(int page = 1, int pageSize = 50)
    {
        try
        {
            var url = $"api/loyalty/transactions?page={page}&pageSize={pageSize}";
            return await _httpClient.GetFromJsonAsync<List<LoyaltyTransactionDto>>(url) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting transactions: {ex.Message}");
            return new List<LoyaltyTransactionDto>();
        }
    }
}

// ================== DTOs ==================

public class LoyaltyPointsDto
{
    public Guid UserId { get; set; }
    public int CurrentPoints { get; set; }
    public int TotalEarned { get; set; }
    public int TotalRedeemed { get; set; }
    public decimal PointsValue { get; set; }
}

public class LoyaltyTransactionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int PointsChange { get; set; }
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid?  OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}
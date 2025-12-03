namespace CampusEats.Backend.Common.DTOs;

public record LoyaltyPointsDto
{
    public Guid UserId { get; init; }
    public int CurrentPoints { get; init; }
    public int TotalEarned { get; init; }
    public int TotalRedeemed { get; init; }
    public decimal PointsValue { get; init; }
}

public record LoyaltyTransactionDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public int PointsChange { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record RedeemPointsResultDto
{
    public int PointsRedeemed { get; init; }
    public decimal DiscountAmount { get; init; }
    public int RemainingPoints { get; init; }
    public string Message { get; init; } = string.Empty;
}
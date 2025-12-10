namespace CampusEats.Backend.Common.DTOs;

public record LoyaltyPointsDto
{
    public Guid UserId { get; init; }
    public decimal CurrentPoints { get; init; }
    public decimal TotalEarned { get; init; }
    public decimal TotalRedeemed { get; init; }
    public decimal PointsValue { get; init; }
}

public record LoyaltyTransactionDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public decimal PointsChange { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record RedeemPointsResultDto
{
    public decimal PointsRedeemed { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal RemainingPoints { get; init; }
    public string Message { get; init; } = string.Empty;
}
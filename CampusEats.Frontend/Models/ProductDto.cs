namespace CampusEats.Frontend.Models;

public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? Category { get; init; }
    public string? ImageUrl { get; init; }

    // IMPORTANT: backend trimite arrays -> păstrăm ca liste
    public List<string>? Allergens { get; init; }
    public List<string>? DietaryRestrictions { get; init; }
    public bool IsAvailable { get; init; } = true;
}
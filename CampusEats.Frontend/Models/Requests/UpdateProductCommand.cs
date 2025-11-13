namespace CampusEats.Frontend.Models.Requests;

public class UpdateProductCommand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? Allergens { get; set; }
    public List<string>? DietaryRestrictions { get; set; }
    public bool IsAvailable { get; set; } = true;

    public UpdateProductCommand() { }

    // A) CSV la pozițiile 7-8
    public UpdateProductCommand(
        Guid id,
        string name,
        string? description,
        decimal price,
        string? category,
        string? imageUrl,
        string? allergensCsv,
        string? dietaryRestrictionsCsv,
        bool isAvailable)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Category = category;
        ImageUrl = imageUrl;
        Allergens = SplitCsv(allergensCsv);
        DietaryRestrictions = SplitCsv(dietaryRestrictionsCsv);
        IsAvailable = isAvailable;
    }

    // B) Liste la pozițiile 7-8 (imageUrl pe 6)
    public UpdateProductCommand(
        Guid id,
        string name,
        string? description,
        decimal price,
        string? category,
        string? imageUrl,
        List<string>? allergens,
        List<string>? dietaryRestrictions,
        bool isAvailable)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Category = category;
        ImageUrl = imageUrl;
        Allergens = allergens;
        DietaryRestrictions = dietaryRestrictions;
        IsAvailable = isAvailable;
    }

    // C) Variante cerute de EditProduct: liste pe pozițiile 6-7, imageUrl la FINAL (poziția 9)
    public UpdateProductCommand(
        Guid id,
        string name,
        string? description,
        decimal price,
        string? category,
        List<string>? allergens,
        List<string>? dietaryRestrictions,
        bool isAvailable,
        string? imageUrl)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Category = category;
        Allergens = allergens;
        DietaryRestrictions = dietaryRestrictions;
        IsAvailable = isAvailable;
        ImageUrl = imageUrl;
    }

    private static List<string>? SplitCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return null;
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(s => s.Trim())
                  .Where(s => !string.IsNullOrWhiteSpace(s))
                  .ToList();
    }
}
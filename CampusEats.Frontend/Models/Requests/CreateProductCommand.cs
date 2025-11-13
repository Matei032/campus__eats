namespace CampusEats.Frontend.Models.Requests;

public class CreateProductCommand
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? Allergens { get; set; }
    public List<string>? DietaryRestrictions { get; set; }
    public bool IsAvailable { get; set; } = true;

    public CreateProductCommand() { }

    // A) CSV la pozițiile 6-7 (dacă pagina trimite string)
    public CreateProductCommand(
        string name,
        string? description,
        decimal price,
        string? category,
        string? imageUrl,
        string? allergensCsv,
        string? dietaryRestrictionsCsv,
        bool isAvailable)
    {
        Name = name;
        Description = description;
        Price = price;
        Category = category;
        ImageUrl = imageUrl;
        Allergens = SplitCsv(allergensCsv);
        DietaryRestrictions = SplitCsv(dietaryRestrictionsCsv);
        IsAvailable = isAvailable;
    }

    // B) Liste la pozițiile 6-7 (pagina ta trimite List<string> pt. alergeni/restricții)
    public CreateProductCommand(
        string name,
        string? description,
        decimal price,
        string? category,
        string? imageUrl,
        List<string>? allergens,
        List<string>? dietaryRestrictions,
        bool isAvailable)
    {
        Name = name;
        Description = description;
        Price = price;
        Category = category;
        ImageUrl = imageUrl;
        Allergens = allergens;
        DietaryRestrictions = dietaryRestrictions;
        IsAvailable = isAvailable;
    }

    // C) Variante cerute de EditProduct: liste la pozițiile 6-7, imageUrl la FINAL
    public CreateProductCommand(
        string name,
        string? description,
        decimal price,
        string? category,
        List<string>? allergens,
        List<string>? dietaryRestrictions,
        bool isAvailable,
        string? imageUrl)
    {
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
using CampusEats.Backend.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Check if data already exists
        if (await context.Products.AnyAsync() || await context.Users.AnyAsync())
        {
            return; // Database already seeded
        }
        
        // SEED USERS
        var users = new List<User>
        {
            new()
            {
                Id = Guid.Parse("f1e2d3c4-b5a6-4948-8372-615849f7a0b1"),
                Email = "student1@campus.ro",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student123!"),
                FullName = "Alex Popescu",
                Role = "Student",
                PhoneNumber = "+40712345678",
                LoyaltyPoints = 150,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("a2b3c4d5-e6f7-5948-9372-715849a8b1c2"),
                Email = "student2@campus.ro",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student123!"),
                FullName = "Maria Ionescu",
                Role = "Student",
                PhoneNumber = "+40723456789",
                LoyaltyPoints = 200,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("b3c4d5e6-f7a8-6059-0483-826950b9c2d3"),
                Email = "staff1@campus.ro",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff123!"),
                FullName = "Ion Georgescu",
                Role = "Staff",
                PhoneNumber = "+40734567890",
                LoyaltyPoints = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("c4d5e6f7-a8b9-716a-1594-937061c0d3e4"),
                Email = "admin@campus.ro",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FullName = "Admin User",
                Role = "Admin",
                PhoneNumber = "+40745678901",
                LoyaltyPoints = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Users.AddRangeAsync(users);
        
        var products = new List<Product>
        {
            // MAIN COURSES
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Burger Classic",
                Description = "Burger cu carne de vită, salată, roșii, castraveți murați și sos special",
                Price = 25.00m,
                Category = "Main",
                ImageUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd",
                Allergens = new List<string> { "Gluten", "Lactose" },
                DietaryRestrictions = null,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Pizza Margherita",
                Description = "Pizza clasică cu sos de roșii, mozzarella și busuioc proaspăt",
                Price = 28.00m,
                Category = "Main",
                ImageUrl = "https://images.unsplash.com/photo-1574071318508-1cdbab80d002",
                Allergens = new List<string> { "Gluten", "Lactose" },
                DietaryRestrictions = "Vegetarian",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Salată Caesar",
                Description = "Mix de salată verde, piept de pui la grătar, crutoane, parmezan și sos Caesar",
                Price = 22.00m,
                Category = "Main",
                ImageUrl = "https://images.unsplash.com/photo-1546793665-c74683f339c1",
                Allergens = new List<string> { "Gluten", "Eggs", "Fish" },
                DietaryRestrictions = null,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Paste Carbonara",
                Description = "Paste cu bacon, ou, parmezan și piper negru",
                Price = 26.00m,
                Category = "Main",
                ImageUrl = "https://images.unsplash.com/photo-1612874742237-6526221588e3",
                Allergens = new List<string> { "Gluten", "Eggs", "Lactose" },
                DietaryRestrictions = null,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Wrap Vegan",
                Description = "Tortilla integrală cu hummus, avocado, roșii, castraveți și salată verde",
                Price = 20.00m,
                Category = "Main",
                ImageUrl = "https://images.unsplash.com/photo-1626700051175-6818013e1d4f",
                Allergens = new List<string> { "Gluten", "Sesame" },
                DietaryRestrictions = "Vegan",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },

            // DRINKS
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Cappuccino",
                Description = "Espresso cu lapte spumat și pudră de cacao",
                Price = 10.00m,
                Category = "Drink",
                ImageUrl = "https://images.unsplash.com/photo-1572442388796-11668a67e53d",
                Allergens = new List<string> { "Lactose" },
                DietaryRestrictions = "Vegetarian",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Limonadă Naturală",
                Description = "Limonadă proaspătă cu lămâie, mentă și miere",
                Price = 12.00m,
                Category = "Drink",
                ImageUrl = "https://images.unsplash.com/photo-1523677011781-c91d1bbe2f9e",
                Allergens = new List<string>(),
                DietaryRestrictions = "Vegan",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Smoothie cu Fructe de Pădure",
                Description = "Smoothie cu afine, mure, căpșuni și banană",
                Price = 15.00m,
                Category = "Drink",
                ImageUrl = "https://images.unsplash.com/photo-1505252585461-04db1eb84625",
                Allergens = new List<string>(),
                DietaryRestrictions = "Vegan",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Apă Minerală",
                Description = "Apă minerală naturală 500ml",
                Price = 5.00m,
                Category = "Drink",
                ImageUrl = "https://images.unsplash.com/photo-1548839140-29a749e1cf4d",
                Allergens = new List<string>(),
                DietaryRestrictions = "Vegan",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },

            // DESSERTS
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Cheesecake cu Fructe de Pădure",
                Description = "Cheesecake cremos cu topping de fructe de pădure",
                Price = 18.00m,
                Category = "Dessert",
                ImageUrl = "https://images.unsplash.com/photo-1533134242443-d4fd215305ad",
                Allergens = new List<string> { "Gluten", "Eggs", "Lactose" },
                DietaryRestrictions = "Vegetarian",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Brownie cu Ciocolată",
                Description = "Brownie cu ciocolată belgiană și nuci",
                Price = 14.00m,
                Category = "Dessert",
                ImageUrl = "https://images.unsplash.com/photo-1606313564200-e75d5e30476c",
                Allergens = new List<string> { "Gluten", "Eggs", "Lactose", "Nuts" },
                DietaryRestrictions = "Vegetarian",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Tiramisu",
                Description = "Desert italian clasic cu mascarpone și cafea",
                Price = 16.00m,
                Category = "Dessert",
                ImageUrl = "https://images.unsplash.com/photo-1571877227200-a0d98ea607e9",
                Allergens = new List<string> { "Gluten", "Eggs", "Lactose" },
                DietaryRestrictions = "Vegetarian",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },

            // SNACKS
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Croissant cu Ciocolată",
                Description = "Croissant frantuzesc cu umplutură de ciocolată",
                Price = 8.00m,
                Category = "Snack",
                ImageUrl = "https://images.unsplash.com/photo-1555507036-ab1f4038808a",
                Allergens = new List<string> { "Gluten", "Eggs", "Lactose" },
                DietaryRestrictions = "Vegetarian",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Sandwich cu Șuncă și Cașcaval",
                Description = "Sandwich simplu cu șuncă, cașcaval, roșii și salată",
                Price = 12.00m,
                Category = "Snack",
                ImageUrl = "https://images.unsplash.com/photo-1528735602780-2552fd46c7af",
                Allergens = new List<string> { "Gluten", "Lactose" },
                DietaryRestrictions = null,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Chips-uri cu Sare de Mare",
                Description = "Chips-uri crocante cu sare de mare",
                Price = 6.00m,
                Category = "Snack",
                ImageUrl = "https://images.unsplash.com/photo-1566478989037-eec170784d0b",
                Allergens = new List<string>(),
                DietaryRestrictions = "Vegan",
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Products.AddRangeAsync(products);
        
        await context.SaveChangesAsync();
    }
}
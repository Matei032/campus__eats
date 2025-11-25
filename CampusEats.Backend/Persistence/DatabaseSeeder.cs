using CampusEats.Backend.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Only check if orders already exist - allow re-seeding products/users if needed
        if (await context.Orders.AnyAsync())
        {
            return; // Database already seeded with orders
        }
        
        // SEED USERS (only if they don't exist)
        if (!await context.Users.AnyAsync())
        {
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
            await context.SaveChangesAsync();
        }

        // SEED PRODUCTS (only if they don't exist)
        if (!await context.Products.AnyAsync())
        {
            var products = new List<Product>
            {
                // MAIN COURSES
                new()
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Fixed ID for easier testing
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
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), // Fixed ID for easier testing
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
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), // Fixed ID for easier testing
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
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), // Fixed ID for easier testing
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
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), // Fixed ID for easier testing
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
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
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
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
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
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
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

                // DESSERTS & SNACKS (keeping some of your original ones)
                new()
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
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
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Name = "Croissant cu Ciocolată",
                    Description = "Croissant frantuzesc cu umplutură de ciocolată",
                    Price = 8.00m,
                    Category = "Snack",
                    ImageUrl = "https://images.unsplash.com/photo-1555507036-ab1f4038808a",
                    Allergens = new List<string> { "Gluten", "Eggs", "Lactose" },
                    DietaryRestrictions = "Vegetarian",
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

        // Get existing users and products for order seeding
        var existingUsers = await context.Users.ToListAsync();
        var existingProducts = await context.Products.ToListAsync();

        // SEED TEST ORDERS FOR KITCHEN FUNCTIONALITY
        var baseDate = DateTime.UtcNow;
        var orders = new List<Order>
        {
            // Pending Orders (for kitchen to start working on)
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-20251112-0001",
                UserId = existingUsers[0].Id, // Alex Popescu
                TotalAmount = 53.00m,
                Status = "Pending",
                PaymentStatus = "Paid",
                PaymentMethod = "Card",
                Notes = "Fără ceapă la burger, vă rog",
                CreatedAt = baseDate.AddMinutes(-25), // 25 minutes ago (high priority)
                UpdatedAt = baseDate.AddMinutes(-25)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-20251112-0002",
                UserId = existingUsers[1].Id, // Maria Ionescu
                TotalAmount = 40.00m,
                Status = "Pending",
                PaymentStatus = "Paid",
                PaymentMethod = "Cash",
                Notes = null,
                CreatedAt = baseDate.AddMinutes(-10), // 10 minutes ago (normal priority)
                UpdatedAt = baseDate.AddMinutes(-10)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-20251112-0003",
                UserId = existingUsers[0].Id, // Alex Popescu
                TotalAmount = 35.00m,
                Status = "Preparing",
                PaymentStatus = "Paid",
                PaymentMethod = "Card",
                Notes = "Extra picant la paste",
                CreatedAt = baseDate.AddMinutes(-20),
                UpdatedAt = baseDate.AddMinutes(-5) // Status updated 5 minutes ago
            },

            // Completed Orders (for inventory reports)
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-20251112-0004",
                UserId = existingUsers[1].Id, // Maria Ionescu
                TotalAmount = 45.00m,
                Status = "Completed",
                PaymentStatus = "Paid",
                PaymentMethod = "Card",
                Notes = null,
                CreatedAt = baseDate.AddHours(-2), // 2 hours ago
                UpdatedAt = baseDate.AddHours(-1.5),
                CompletedAt = baseDate.AddHours(-1.5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-20251112-0005",
                UserId = existingUsers[0].Id, // Alex Popescu
                TotalAmount = 30.00m,
                Status = "Completed",
                PaymentStatus = "Paid",
                PaymentMethod = "Cash",
                Notes = null,
                CreatedAt = baseDate.AddHours(-1),
                UpdatedAt = baseDate.AddMinutes(-30),
                CompletedAt = baseDate.AddMinutes(-30)
            },

            // Ready for pickup
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-20251112-0006",
                UserId = existingUsers[1].Id, // Maria Ionescu
                TotalAmount = 22.00m,
                Status = "Ready",
                PaymentStatus = "Paid",
                PaymentMethod = "Card",
                Notes = "Dressing separat pentru salată",
                CreatedAt = baseDate.AddMinutes(-15),
                UpdatedAt = baseDate.AddMinutes(-2)
            }
        };

        await context.Orders.AddRangeAsync(orders);
        await context.SaveChangesAsync(); // Save orders so we can reference them in order items

        // SEED ORDER ITEMS
        var orderItems = new List<OrderItem>
        {
            // Order 1 (Pending) - Burger + Cappuccino + Cheesecake
            new() { Id = Guid.NewGuid(), OrderId = orders[0].Id, ProductId = existingProducts[0].Id, Quantity = 1, UnitPrice = 25.00m, Subtotal = 25.00m, SpecialInstructions = "Fără ceapă" },
            new() { Id = Guid.NewGuid(), OrderId = orders[0].Id, ProductId = existingProducts[5].Id, Quantity = 1, UnitPrice = 10.00m, Subtotal = 10.00m },
            new() { Id = Guid.NewGuid(), OrderId = orders[0].Id, ProductId = existingProducts[8].Id, Quantity = 1, UnitPrice = 18.00m, Subtotal = 18.00m },

            // Order 2 (Pending) - Pizza + Limonadă
            new() { Id = Guid.NewGuid(), OrderId = orders[1].Id, ProductId = existingProducts[1].Id, Quantity = 1, UnitPrice = 28.00m, Subtotal = 28.00m },
            new() { Id = Guid.NewGuid(), OrderId = orders[1].Id, ProductId = existingProducts[6].Id, Quantity = 1, UnitPrice = 12.00m, Subtotal = 12.00m },

            // Order 3 (Preparing) - Carbonara + Smoothie
            new() { Id = Guid.NewGuid(), OrderId = orders[2].Id, ProductId = existingProducts[3].Id, Quantity = 1, UnitPrice = 26.00m, Subtotal = 26.00m, SpecialInstructions = "Extra bacon" },
            new() { Id = Guid.NewGuid(), OrderId = orders[2].Id, ProductId = existingProducts[7].Id, Quantity = 1, UnitPrice = 15.00m, Subtotal = 15.00m },

            // Order 4 (Completed) - Wrap + Cappuccino + Croissant
            new() { Id = Guid.NewGuid(), OrderId = orders[3].Id, ProductId = existingProducts[4].Id, Quantity = 1, UnitPrice = 20.00m, Subtotal = 20.00m },
            new() { Id = Guid.NewGuid(), OrderId = orders[3].Id, ProductId = existingProducts[5].Id, Quantity = 2, UnitPrice = 10.00m, Subtotal = 20.00m },
            new() { Id = Guid.NewGuid(), OrderId = orders[3].Id, ProductId = existingProducts[9].Id, Quantity = 1, UnitPrice = 8.00m, Subtotal = 8.00m },

            // Order 5 (Completed) - Caesar Salad + Smoothie
            new() { Id = Guid.NewGuid(), OrderId = orders[4].Id, ProductId = existingProducts[2].Id, Quantity = 1, UnitPrice = 22.00m, Subtotal = 22.00m },
            new() { Id = Guid.NewGuid(), OrderId = orders[4].Id, ProductId = existingProducts[7].Id, Quantity = 1, UnitPrice = 15.00m, Subtotal = 15.00m },

            // Order 6 (Ready) - Caesar Salad only
            new() { Id = Guid.NewGuid(), OrderId = orders[5].Id, ProductId = existingProducts[2].Id, Quantity = 1, UnitPrice = 22.00m, Subtotal = 22.00m, SpecialInstructions = "Dressing separat" }
        };
        
        var payments = new List<Payment>
    {
        new()
        {
            Id = Guid.NewGuid(),
            OrderId = orders[0].Id,
            Amount = orders[0].TotalAmount,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Card,
            StripePaymentIntentId = "simulated_101",
            CreatedAt = orders[0].CreatedAt,
            PaidAt = orders[0].CreatedAt.AddMinutes(2),
            LoyaltyPointsUsed = null
        },
        new()
        {
            Id = Guid.NewGuid(),
            OrderId = orders[1].Id,
            Amount = orders[1].TotalAmount,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Cash,
            CreatedAt = orders[1].CreatedAt,
            PaidAt = orders[1].CreatedAt.AddMinutes(1),
            LoyaltyPointsUsed = null
        },
        new()
        {
            Id = Guid.NewGuid(),
            OrderId = orders[2].Id,
            Amount = orders[2].TotalAmount,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Card,
            StripePaymentIntentId = "simulated_102",
            CreatedAt = orders[2].CreatedAt,
            PaidAt = orders[2].CreatedAt.AddMinutes(3),
            LoyaltyPointsUsed = null
        },
        new()
        {
            Id = Guid.NewGuid(),
            OrderId = orders[3].Id,
            Amount = orders[3].TotalAmount,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Card,
            StripePaymentIntentId = "simulated_103",
            CreatedAt = orders[3].CreatedAt,
            PaidAt = orders[3].CreatedAt.AddMinutes(4),
            LoyaltyPointsUsed = null
        },
        new()
        {
            Id = Guid.NewGuid(),
            OrderId = orders[4].Id,
            Amount = orders[4].TotalAmount,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Cash,
            CreatedAt = orders[4].CreatedAt,
            PaidAt = orders[4].CreatedAt.AddMinutes(1),
            LoyaltyPointsUsed = null
        },
        new()
        {
            Id = Guid.NewGuid(),
            OrderId = orders[5].Id,
            Amount = orders[5].TotalAmount,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Card,
            StripePaymentIntentId = "simulated_104",
            CreatedAt = orders[5].CreatedAt,
            PaidAt = orders[5].CreatedAt.AddMinutes(2),
            LoyaltyPointsUsed = null
        }
    };

        await context.OrderItems.AddRangeAsync(orderItems);
        await context.SaveChangesAsync();
    }
}
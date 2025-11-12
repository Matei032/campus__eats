using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Orders;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Orders;

public class CreateOrderTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithOrder()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CreateOrder.Handler(context);

        // Create test user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // Create test products
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Burger",
            Description = "Test description",
            Price = 25.00m,
            Category = "Main",
            Allergens = new List<string> { "Gluten" },
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Coffee",
            Description = "Test description",
            Price = 10.00m,
            Category = "Drink",
            Allergens = new List<string>(),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.AddRange(product1, product2);
        await context.SaveChangesAsync();

        // Create command
        var command = new CreateOrder.Command
        {
            UserId = user.Id,
            PaymentMethod = "Card",
            Notes = "Test order",
            Items = new List<CreateOrder.OrderItemRequest>
            {
                new() { ProductId = product1.Id, Quantity = 2, SpecialInstructions = "No pickles" },
                new() { ProductId = product2.Id, Quantity = 1, SpecialInstructions = null }
            }
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(user.Id);
        result.Value.TotalAmount.Should().Be(60.00m); // 2*25 + 1*10 = 60
        result.Value.Status.Should().Be("Pending");
        result.Value.PaymentStatus.Should().Be("Pending");
        result.Value.OrderNumber.Should().StartWith("ORD-");
        result.Value.OrderItems.Should().HaveCount(2);
        result.Value.OrderItems[0].ProductName.Should().Be("Test Burger");
        result.Value.OrderItems[0].Subtotal.Should().Be(50.00m);
    }

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CreateOrder.Handler(context);

        var command = new CreateOrder.Command
        {
            UserId = Guid.NewGuid(), // Non-existent user
            Items = new List<CreateOrder.OrderItemRequest>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("User") && e.Contains("not found"));
    }

    [Fact]
    public async Task Handle_InvalidProductId_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CreateOrder.Handler(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var command = new CreateOrder.Command
        {
            UserId = user.Id,
            Items = new List<CreateOrder.OrderItemRequest>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 1 } // Non-existent product
            }
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Products not found"));
    }

    [Fact]
    public async Task Handle_UnavailableProduct_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CreateOrder.Handler(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Unavailable Product",
            Description = "Test",
            Price = 10.00m,
            Category = "Main",
            Allergens = new List<string>(),
            IsAvailable = false, // ← Not available!
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var command = new CreateOrder.Command
        {
            UserId = user.Id,
            Items = new List<CreateOrder.OrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 1 }
            }
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not available"));
    }

    [Fact]
    public async Task Handle_ValidCommand_CalculatesTotalAmountCorrectly()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new CreateOrder.Handler(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Test",
            Price = 12.50m,
            Category = "Main",
            Allergens = new List<string>(),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var command = new CreateOrder.Command
        {
            UserId = user.Id,
            Items = new List<CreateOrder.OrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 3 }
            }
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalAmount.Should().Be(37.50m); // 3 * 12.50 = 37.50
        result.Value.OrderItems[0].Subtotal.Should().Be(37.50m);
    }
}
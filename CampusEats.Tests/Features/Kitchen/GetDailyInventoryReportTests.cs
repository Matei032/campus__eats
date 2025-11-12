using CampusEats.Backend.Features.Kitchen;
using CampusEats.Backend.Persistence;
using CampusEats.Backend.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Backend.Tests.Features.Kitchen;

public class GetDailyInventoryReportTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_Generate_Correct_Daily_Report()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var reportDate = DateTime.UtcNow.Date;
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var burger = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Burger",
            Price = 25.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var pizza = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Pizza",
            Price = 30.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var coffee = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Coffee",
            Price = 10.00m,
            Category = "Drink",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        // Completed orders from today
        var order1 = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 60.00m,
            CreatedAt = reportDate.AddHours(10),
            CompletedAt = reportDate.AddHours(10.5)
        };

        var order2 = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-002",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 35.00m,
            CreatedAt = reportDate.AddHours(14),
            CompletedAt = reportDate.AddHours(14.5)
        };

        // Order from different day (should be excluded)
        var yesterdayOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-003",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 100.00m,
            CreatedAt = reportDate.AddDays(-1),
            CompletedAt = reportDate.AddDays(-1)
        };

        await context.Users.AddAsync(user);
        await context.Products.AddRangeAsync(burger, pizza, coffee);
        await context.Orders.AddRangeAsync(order1, order2, yesterdayOrder);
        await context.SaveChangesAsync();

        // Order items for today's orders
        var orderItems = new List<OrderItem>
        {
            new() { Id = Guid.NewGuid(), OrderId = order1.Id, ProductId = burger.Id, Quantity = 2, UnitPrice = 25.00m, Subtotal = 50.00m },
            new() { Id = Guid.NewGuid(), OrderId = order1.Id, ProductId = coffee.Id, Quantity = 1, UnitPrice = 10.00m, Subtotal = 10.00m },
            new() { Id = Guid.NewGuid(), OrderId = order2.Id, ProductId = burger.Id, Quantity = 1, UnitPrice = 25.00m, Subtotal = 25.00m },
            new() { Id = Guid.NewGuid(), OrderId = order2.Id, ProductId = coffee.Id, Quantity = 1, UnitPrice = 10.00m, Subtotal = 10.00m }
        };

        await context.OrderItems.AddRangeAsync(orderItems);
        await context.SaveChangesAsync();

        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = reportDate };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var report = result.Value!;
        report.ReportDate.Should().Be(reportDate);
        report.TotalRevenue.Should().Be(95.00m); // Only today's orders
        report.TotalOrdersProcessed.Should().Be(2);
        report.TotalItemsSold.Should().Be(5); // 2 + 1 + 1 + 1

        // Check individual product statistics
        var burgerStats = report.InventoryItems.First(i => i.ProductName == "Burger");
        burgerStats.QuantitySold.Should().Be(3); // 2 + 1
        burgerStats.Revenue.Should().Be(75.00m); // 50 + 25
        burgerStats.OrderCount.Should().Be(2);

        var coffeeStats = report.InventoryItems.First(i => i.ProductName == "Coffee");
        coffeeStats.QuantitySold.Should().Be(2); // 1 + 1
        coffeeStats.Revenue.Should().Be(20.00m); // 10 + 10
        coffeeStats.OrderCount.Should().Be(2);

        var pizzaStats = report.InventoryItems.First(i => i.ProductName == "Pizza");
        pizzaStats.QuantitySold.Should().Be(0); // Not sold today
        pizzaStats.Revenue.Should().Be(0);
        pizzaStats.OrderCount.Should().Be(0);

        // Should be ordered by quantity sold descending
        report.InventoryItems.First().ProductName.Should().Be("Burger");
    }

    [Fact]
    public async Task Handle_Should_Use_Current_Date_When_No_Date_Specified()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = null };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ReportDate.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task Handle_Should_Only_Include_Completed_Orders()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var reportDate = DateTime.UtcNow.Date;
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 20.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var pendingOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Pending",
            PaymentStatus = "Pending",
            TotalAmount = 20.00m,
            CreatedAt = reportDate.AddHours(10)
        };

        var completedOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-002",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 20.00m,
            CreatedAt = reportDate.AddHours(11)
        };

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddRangeAsync(pendingOrder, completedOrder);
        await context.SaveChangesAsync();

        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = reportDate };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalOrdersProcessed.Should().Be(1); // Only completed order
        result.Value.TotalRevenue.Should().Be(20.00m);
    }
}
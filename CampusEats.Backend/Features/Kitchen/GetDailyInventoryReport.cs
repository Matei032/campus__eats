using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Kitchen;

public static class GetDailyInventoryReport
{
    public record Query : IRequest<Result<InventoryReportDto>>
    {
        public DateTime? ReportDate { get; init; } = null;
    }
    
    internal sealed class Handler : IRequestHandler<Query, Result<InventoryReportDto>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<InventoryReportDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var reportDate = (request.ReportDate?.Date ?? DateTime.UtcNow.Date);
            // Ensure UTC kind
            reportDate = DateTime.SpecifyKind(reportDate, DateTimeKind.Utc);
            
            var nextDay = reportDate.AddDays(1);
    
                // --- MODIFICARE IMPORTANTĂ ---
                // Căutăm comenzi FINALIZATE în ziua respectivă (CompletedAt), nu create.
            // Postgres requires UTC dates for timestamp with time zone
            var dailyOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Completed" && 
                            o.CompletedAt >= reportDate && 
                            o.CompletedAt < nextDay)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync(cancellationToken);
                // -----------------------------
    
                var allProducts = await _context.Products
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
    
                var inventoryItems = allProducts.Select(product =>
                {
                    var productOrderItems = dailyOrders
                        .SelectMany(o => o.OrderItems)
                        .Where(oi => oi.ProductId == product.Id)
                        .ToList();
    
                    var quantitySold = productOrderItems.Sum(oi => oi.Quantity);
                    var revenue = productOrderItems.Sum(oi => oi.Subtotal);
                    var orderCount = productOrderItems.Count;
                    var averageOrderValue = orderCount > 0 ? revenue / orderCount : 0;
    
                    // Includem în listă doar produsele care s-au vândut
                    if (quantitySold == 0) return null;
    
                    return new InventoryItemDto
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        QuantitySold = quantitySold,
                        UnitPrice = product.Price,
                        Subtotal = revenue,
                        Revenue = revenue,
                        OrderCount = orderCount,
                        AverageOrderValue = averageOrderValue
                    };
                })
                .Where(i => i != null)
                .Cast<InventoryItemDto>()
                .OrderByDescending(i => i.QuantitySold)
                .ToList();
    
                var totalRevenue = dailyOrders.Sum(o => o.TotalAmount);
                var totalOrdersProcessed = dailyOrders.Count;
                var totalItemsSold = inventoryItems.Sum(i => i.QuantitySold);
    
                var inventoryReport = new InventoryReportDto
                {
                    ReportDate = reportDate,
                    InventoryItems = inventoryItems,
                    TotalRevenue = totalRevenue,
                    TotalOrdersProcessed = totalOrdersProcessed,
                    TotalItemsSold = totalItemsSold
                };
    
                return Result<InventoryReportDto>.Success(inventoryReport);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Inventory Error] {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw; // Rethrow to let the controller handle 500 but now it's logged
            }
        }
    }
}
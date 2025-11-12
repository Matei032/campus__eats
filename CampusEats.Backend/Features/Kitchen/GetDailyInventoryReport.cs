using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Kitchen;

public static class GetDailyInventoryReport
{
    // QUERY
    public record Query : IRequest<Result<InventoryReportDto>>
    {
        public DateTime? ReportDate { get; init; } = null; // Defaults to today
    }
    
    // VALIDATOR
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReportDate)
                .LessThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage("Report date cannot be in the future")
                .When(x => x.ReportDate.HasValue);
        }
    }
    
    // HANDLER
    internal sealed class Handler : IRequestHandler<Query, Result<InventoryReportDto>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<InventoryReportDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var reportDate = request.ReportDate?.Date ?? DateTime.UtcNow.Date;
            var nextDay = reportDate.AddDays(1);

            // Get all orders for the specified date (completed orders only for accurate reporting)
            var dailyOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= reportDate && o.CreatedAt < nextDay && o.Status == "Completed")
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync(cancellationToken);

            // Get all products for complete inventory view
            var allProducts = await _context.Products
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Calculate inventory statistics per product
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

                return new InventoryItemDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Category = product.Category,
                    QuantitySold = quantitySold,
                    Revenue = revenue,
                    OrderCount = orderCount,
                    AverageOrderValue = averageOrderValue
                };
            })
            .OrderByDescending(i => i.QuantitySold) // Most sold products first
            .ToList();

            // Calculate daily totals
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
    }
}
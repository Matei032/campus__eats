using CampusEats.Backend.Persistence;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Kitchen;

public static class GetDailyInventoryReport
{
    public record Query : IRequest<Result<DailyInventoryReportDto>>
    {
        public DateTime? ReportDate { get; init; }
    }

    public class DailyInventoryReportDto
    {
        public DateTime ReportDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrdersProcessed { get; set; }
        public int TotalItemsSold { get; set; }
        public List<InventoryItemDto> InventoryItems { get; set; } = new();
    }

    public class InventoryItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    internal sealed class Handler : IRequestHandler<Query, Result<DailyInventoryReportDto>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<DailyInventoryReportDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var reportDate = (request.ReportDate ?? DateTime.UtcNow).Date;

            // Load today's completed orders (Completed status and CompletedAt date matches reportDate)
            var orders = await _context.Orders
                .Where(o => o.Status == "Completed" && o.CompletedAt.HasValue && o.CompletedAt.Value.Date == reportDate)
                .ToListAsync(cancellationToken);

            // If no orders, still return a report with zeros and include all products (so tests expecting pizza with zeros pass)
            var orderIds = orders.Select(o => o.Id).ToHashSet();

            // Load relevant order items (only those that belong to today's completed orders)
            var orderItems = await _context.OrderItems
                .Where(oi => orderIds.Contains(oi.OrderId))
                .ToListAsync(cancellationToken);

            // Load all products to include even those with zero sales
            var products = await _context.Products
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            var report = new DailyInventoryReportDto
            {
                ReportDate = reportDate,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                TotalOrdersProcessed = orders.Count,
                TotalItemsSold = orderItems.Sum(oi => oi.Quantity)
            };

            var inventory = new List<InventoryItemDto>();

            foreach (var product in products)
            {
                var itemsForProduct = orderItems.Where(oi => oi.ProductId == product.Id).ToList();
                var quantitySold = itemsForProduct.Sum(i => i.Quantity);
                var revenue = itemsForProduct.Sum(i => i.Subtotal);
                var orderCount = itemsForProduct.Select(i => i.OrderId).Distinct().Count();

                inventory.Add(new InventoryItemDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    QuantitySold = quantitySold,
                    Revenue = revenue,
                    OrderCount = orderCount
                });
            }

            // order by quantity sold desc so the most sold product is first (test expects Burger first)
            report.InventoryItems = inventory
                .OrderByDescending(i => i.QuantitySold)
                .ThenBy(i => i.ProductName)
                .ToList();

            return Result<DailyInventoryReportDto>.Success(report);
        }
    }
}
using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Kitchen;

public static class GetPendingOrders
{
    // QUERY
    public record Query : IRequest<Result<List<OrderDto>>>
    {
        // No parameters
    }
    
    // HANDLER
    internal sealed class Handler : IRequestHandler<Query, Result<List<OrderDto>>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<OrderDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Pending" || o.Status == "Preparing" || o.Status == "Ready") 
                .OrderBy(o => o.CreatedAt)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product) // <--- CRITIC: Includem produsul pentru a-i afla numele
                .ToListAsync(cancellationToken);

            var orderDtos = orders.Select(order => new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                PaymentMethod = order.PaymentMethod,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                CompletedAt = order.CompletedAt,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    // ACUM FOLOSIM NUMELE REAL DIN BAZA DE DATE
                    ProductName = oi.Product != null ? oi.Product.Name : "Produs necunoscut", 
                    Quantity = oi.Quantity,
                    SpecialInstructions = oi.SpecialInstructions
                }).ToList()
            }).ToList();

            return Result<List<OrderDto>>.Success(orderDtos);
        }
    }
}
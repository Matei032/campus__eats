using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Kitchen;

public static class UpdateOrderStatus
{
    public record Command : IRequest<Result<OrderDto>>
    {
        public Guid OrderId { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    internal sealed class Handler : IRequestHandler<Command, Result<OrderDto>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<OrderDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product) // <--- CRITIC: Includem produsul
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order is null)
            {
                return Result<OrderDto>.Failure($"Order {request.OrderId} not found");
            }

            // --- LOGICA DE STATUS ---
            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            if (request.Status == "Completed")
            {
                order.CompletedAt = DateTime.UtcNow;
                if (order.PaymentStatus == "Pending") order.PaymentStatus = "Paid";
            }
            // ------------------------

            await _context.SaveChangesAsync(cancellationToken);

            var orderDto = new OrderDto
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
                    // FOLOSIM NUMELE REAL
                    ProductName = oi.Product != null ? oi.Product.Name : "Produs necunoscut",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.Subtotal,
                    SpecialInstructions = oi.SpecialInstructions
                }).ToList()
            };

            return Result<OrderDto>.Success(orderDto);
        }
    }
}
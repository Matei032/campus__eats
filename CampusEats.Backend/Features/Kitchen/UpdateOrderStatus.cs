using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CampusEats.Backend.Domain;

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
            var wasCompleted = order.Status == "Completed"; // Verificăm dacă era deja completată
            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            if (request.Status == "Completed")
            {
                order.CompletedAt = DateTime.UtcNow;
                if (order.PaymentStatus == "Pending") order.PaymentStatus = "Paid";

                // 🎁 ACORDĂ PUNCTE LOYALTY AUTOMAT (doar prima dată când devine Completed)
                if (!wasCompleted)
                {
                    // Calculăm suma plătită efectiv (fără puncte)
                    var loyaltyPayments = await _context.Payments
                        .Where(p => p.OrderId == order.Id && p.Method == PaymentMethod.LoyaltyPoints)
                        .SumAsync(p => p.Amount, cancellationToken);

                    var netAmount = order.TotalAmount - loyaltyPayments;
                    
                    // 10% din suma plătită efectiv (ex: 22 RON -> 2.2 puncte)
                    var pointsToAward = netAmount * 0.10m;

                    var user = await _context.Users.FindAsync(new object[] { order.UserId }, cancellationToken);
                    if (user != null)
                    {
                        user.LoyaltyPoints += pointsToAward;
                        user.UpdatedAt = DateTime.UtcNow;

                        var loyaltyTransaction = new LoyaltyTransaction
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            PointsChange = pointsToAward,
                            Type = LoyaltyTransactionType.Earned,
                            Description = $"Earned {pointsToAward:F2} points from order #{order.OrderNumber}",
                            OrderId = order.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.LoyaltyTransactions.Add(loyaltyTransaction);
                    }
                }
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
using CampusEats.Backend.Persistence;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
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

        // Define allowed transitions based on business rules used by tests
        private static readonly Dictionary<string, string[]> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Pending"] = new[] { "Preparing", "Cancelled" },
            ["Preparing"] = new[] { "Ready", "Cancelled" },
            ["Ready"] = new[] { "Completed", "Cancelled" },
            // Completed and Cancelled are final - no transitions allowed
            ["Completed"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        };

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<OrderDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order is null)
                return Result<OrderDto>.Failure("Order not found");

            var currentStatus = order.Status ?? string.Empty;
            var newStatus = request.Status ?? string.Empty;

            // If order already completed or cancelled, disallow changes
            if (string.Equals(currentStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                return Result<OrderDto>.Failure("Cannot change status of completed order");

            if (string.Equals(currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                return Result<OrderDto>.Failure("Cannot change status of cancelled order");

            // Validate allowed transitions
            if (!AllowedTransitions.TryGetValue(currentStatus, out var allowedTargets) || !allowedTargets.Contains(newStatus, StringComparer.OrdinalIgnoreCase))
            {
                return Result<OrderDto>.Failure("Invalid status transition");
            }

            // Apply transition
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            if (string.Equals(newStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                order.PaymentStatus = "Paid";
                order.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Map to OrderDto (use existing DTO in Common.DTOs)
            var dto = new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                CompletedAt = order.CompletedAt,
                TotalAmount = order.TotalAmount
            };

            return Result<OrderDto>.Success(dto);
        }
    }
}
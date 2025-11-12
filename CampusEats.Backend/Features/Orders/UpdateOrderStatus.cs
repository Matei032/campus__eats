using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Orders;

public static class UpdateOrderStatus
{
    // COMMAND
    public record Command : IRequest<Result<OrderDto>>
    {
        public Guid OrderId { get; init; }
        public string Status { get; init; } = string.Empty;
    }
    
    // VALIDATOR
    public class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidStatuses = { "Pending", "Preparing", "Ready", "Completed", "Cancelled" };

        public Validator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Order ID is required");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required")
                .Must(status => ValidStatuses.Contains(status))
                .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}");
        }
    }
    
    // HANDLER
    internal sealed class Handler : IRequestHandler<Command, Result<OrderDto>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<OrderDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            // 1. Get order with related data
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order is null)
            {
                return Result<OrderDto>.Failure($"Order with ID {request.OrderId} not found");
            }

            // 2. Validate status transition
            var validationResult = ValidateStatusTransition(order.Status, request.Status);
            if (!string.IsNullOrEmpty(validationResult))
            {
                return Result<OrderDto>.Failure(validationResult);
            }

            // 3. Update order status
            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            // 4. If completed, set completion timestamp
            if (request.Status == "Completed")
            {
                order.CompletedAt = DateTime.UtcNow;
                order.PaymentStatus = "Paid";  // Auto-mark as paid when completed
            }

            // 5. Save changes
            await _context.SaveChangesAsync(cancellationToken);

            // 6. Map to DTO
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
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.Subtotal,
                    SpecialInstructions = oi.SpecialInstructions
                }).ToList()
            };

            return Result<OrderDto>.Success(orderDto);
        }

        // Helper: Validate status transitions
        private static string? ValidateStatusTransition(string currentStatus, string newStatus)
        {
            // Valid transitions:
            // Pending → Preparing, Cancelled
            // Preparing → Ready, Cancelled
            // Ready → Completed, Cancelled
            // Completed → (no transitions)
            // Cancelled → (no transitions)

            if (currentStatus == newStatus)
            {
                return null; // Same status is OK (idempotent)
            }

            return (currentStatus, newStatus) switch
            {
                ("Pending", "Preparing") => null,
                ("Pending", "Cancelled") => null,
                ("Preparing", "Ready") => null,
                ("Preparing", "Cancelled") => null,
                ("Ready", "Completed") => null,
                ("Ready", "Cancelled") => null,
                ("Completed", _) => "Cannot change status of completed order",
                ("Cancelled", _) => "Cannot change status of cancelled order",
                _ => $"Invalid status transition from {currentStatus} to {newStatus}"
            };
        }
    }
}
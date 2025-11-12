using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Orders;

public static class CreateOrder
{
    // COMMAND
    public record Command : IRequest<Result<OrderDto>>
    {
        public Guid UserId { get; init; }
        public string? PaymentMethod { get; init; }
        public string? Notes { get; init; }
        public List<OrderItemRequest> Items { get; init; } = new();
    }

    public record OrderItemRequest
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
        public string? SpecialInstructions { get; init; }
    }
    
    // VALIDATOR
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Order must contain at least one item")
                .Must(items => items.Count > 0).WithMessage("Order must contain at least one item");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId)
                    .NotEmpty().WithMessage("Product ID is required");

                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than 0")
                    .LessThanOrEqualTo(10).WithMessage("Maximum quantity per item is 10");
            });

            RuleFor(x => x.PaymentMethod)
                .Must(pm => string.IsNullOrEmpty(pm) || new[] { "Card", "Cash", "Loyalty" }.Contains(pm))
                .When(x => !string.IsNullOrEmpty(x.PaymentMethod))
                .WithMessage("Payment method must be one of: Card, Cash, Loyalty");
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
            // 1. Validate user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                return Result<OrderDto>.Failure($"User with ID {request.UserId} not found");
            }

            // 2. Get all product IDs from request
            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

            // 3. Fetch products from database
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            // 4. Validate all products exist
            if (products.Count != productIds.Count)
            {
                var missingIds = productIds.Except(products.Select(p => p.Id)).ToList();
                return Result<OrderDto>.Failure($"Products not found: {string.Join(", ", missingIds)}");
            }

            // 5. Validate all products are available
            var unavailableProducts = products.Where(p => !p.IsAvailable).ToList();
            if (unavailableProducts.Any())
            {
                var unavailableNames = string.Join(", ", unavailableProducts.Select(p => p.Name));
                return Result<OrderDto>.Failure($"Products not available: {unavailableNames}");
            }

            // 6. Create order entity
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = GenerateOrderNumber(),
                UserId = request.UserId,
                Status = "Pending",
                PaymentStatus = "Pending",
                PaymentMethod = request.PaymentMethod,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            // 7. Create order items with price snapshot
            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var itemRequest in request.Items)
            {
                var product = products.First(p => p.Id == itemRequest.ProductId);
                var subtotal = product.Price * itemRequest.Quantity;

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = product.Price,
                    Subtotal = subtotal,
                    SpecialInstructions = itemRequest.SpecialInstructions
                };

                orderItems.Add(orderItem);
                totalAmount += subtotal;
            }

            order.TotalAmount = totalAmount;
            order.OrderItems = orderItems;

            // 8. Save to database
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            // 9. Map to DTO
            var dto = new OrderDto
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
                OrderItems = orderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = products.First(p => p.Id == oi.ProductId).Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.Subtotal,
                    SpecialInstructions = oi.SpecialInstructions
                }).ToList()
            };

            return Result<OrderDto>.Success(dto);
        }

        // Helper method: Generate unique order number
        private static string GenerateOrderNumber()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"ORD-{date}-{random}";
        }
    }
}
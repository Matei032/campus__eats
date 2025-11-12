using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Orders;

public static class GetOrderById
{
    // QUERY
    public record Query : IRequest<Result<OrderDto>>
    {
        public Guid OrderId { get; init; }
        public Guid UserId { get; init; }  // For authorization
    }
    
    // HANDLER
    internal sealed class Handler : IRequestHandler<Query, Result<OrderDto>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<OrderDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            // 1. Get order with order items and products
            var order = await _context.Orders
                .AsNoTracking()  // Read-only query
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            // 2. Validate order exists
            if (order is null)
            {
                return Result<OrderDto>.Failure($"Order with ID {request.OrderId} not found");
            }

            // 3. Validate user owns this order
            if (order.UserId != request.UserId)
            {
                return Result<OrderDto>.Failure("You are not authorized to view this order");
            }

            // 4. Map to DTO
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
    }
}
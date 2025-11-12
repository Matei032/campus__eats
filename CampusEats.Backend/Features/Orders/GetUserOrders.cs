using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Orders;

public static class GetUserOrders
{
    // QUERY
    public record Query : IRequest<Result<List<OrderDto>>>
    {
        public Guid UserId { get; init; }
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
            // 1. Validate user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                return Result<List<OrderDto>>.Failure($"User with ID {request.UserId} not found");
            }

            // 2. Get user's orders with order items and products
            var orders = await _context.Orders
                .AsNoTracking()  // Read-only query
                .Where(o => o.UserId == request.UserId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)  // Latest first
                .ToListAsync(cancellationToken);

            // 3. Map to DTOs
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
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.Subtotal,
                    SpecialInstructions = oi.SpecialInstructions
                }).ToList()
            }).ToList();

            return Result<List<OrderDto>>.Success(orderDtos);
        }
    }
}
using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Loyalty;

public static class GetLoyaltyTransactions
{
    public record Query(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<Result<List<LoyaltyTransactionDto>>>;

    public class Handler : IRequestHandler<Query, Result<List<LoyaltyTransactionDto>>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<LoyaltyTransactionDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                return Result<List<LoyaltyTransactionDto>>.Failure("User not found");

            var transactions = await _context.LoyaltyTransactions
                .Where(t => t.UserId == request.UserId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new LoyaltyTransactionDto
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    PointsChange = t.PointsChange,
                    Type = t.Type.ToString(),
                    Description = t.Description,
                    OrderId = t.OrderId,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return Result<List<LoyaltyTransactionDto>>.Success(transactions);
        }
    }
}
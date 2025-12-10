using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Loyalty;

public static class GetLoyaltyPoints
{
    public record Query(Guid UserId) : IRequest<Result<LoyaltyPointsDto>>;

    public class Handler : IRequestHandler<Query, Result<LoyaltyPointsDto>>
    {
        private readonly AppDbContext _context;
        private const decimal PointsToMoneyRatio = 1.0m;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<LoyaltyPointsDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .Include(u => u.LoyaltyTransactions)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                return Result<LoyaltyPointsDto>.Failure("User not found");

            var totalEarned = user.LoyaltyTransactions
                .Where(t => t.Type == Domain.LoyaltyTransactionType.Earned)
                .Sum(t => t.PointsChange);

            var totalRedeemed = Math.Abs(user.LoyaltyTransactions
                .Where(t => t.Type == Domain.LoyaltyTransactionType.Redeemed)
                .Sum(t => t.PointsChange));

            var dto = new LoyaltyPointsDto
            {
                UserId = user.Id,
                CurrentPoints = user.LoyaltyPoints,
                TotalEarned = totalEarned,
                TotalRedeemed = totalRedeemed,
                PointsValue = user.LoyaltyPoints * PointsToMoneyRatio
            };

            return Result<LoyaltyPointsDto>.Success(dto);
        }
    }
}
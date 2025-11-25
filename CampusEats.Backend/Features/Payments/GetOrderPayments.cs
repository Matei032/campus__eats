using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Payments;

public static class GetOrderPayments
{
    public record Query(Guid OrderId) : IRequest<List<PaymentDto>>;

    internal sealed class Handler : IRequestHandler<Query, List<PaymentDto>>
    {
        private readonly AppDbContext _context;
        public Handler(AppDbContext context) => _context = context;

        public async Task<List<PaymentDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var payments = await _context.Payments
                .Where(x => x.OrderId == request.OrderId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    OrderId = p.OrderId,
                    Amount = p.Amount,
                    Status = p.Status,
                    Method = p.Method,
                    StripePaymentIntentId = p.StripePaymentIntentId,
                    StripeSessionId = p.StripeSessionId,
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt,
                    FailedAt = p.FailedAt,
                    FailureReason = p.FailureReason,
                    LoyaltyPointsUsed = p.LoyaltyPointsUsed
                })
                .ToListAsync(cancellationToken);

            return payments;
        }
    }
}
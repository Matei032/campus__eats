using CampusEats.Backend.Common;
using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Payments;

public static class GetAllPayments
{
    public record Query(
        int Page = 1,
        int PageSize = 20,
        string? Status = null,
        string? Method = null
    ) : IRequest<Result<PagedResult<PaymentDto>>>;

    internal sealed class Handler : IRequestHandler<Query, Result<PagedResult<PaymentDto>>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context) => _context = context;

        public async Task<Result<PagedResult<PaymentDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _context.Payments.AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<PaymentStatus>(request.Status, true, out var status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrWhiteSpace(request.Method) &&
                Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
                query = query.Where(p => p.Method == method);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query.OrderByDescending(p => p.CreatedAt)
                                   .Skip((request.Page - 1) * request.PageSize)
                                   .Take(request.PageSize)
                                   .Select(p => new PaymentDto
                                   {
                                       Id = p.Id,
                                       OrderId = p.OrderId,
                                       Amount = p.Amount,
                                       Status = p.Status,
                                       Method = p.Method,
                                       StripePaymentIntentId = p.StripePaymentIntentId,
                                       StripeSessionId = p.StripeSessionId,
                                       StripeClientSecret = null,
                                       CreatedAt = p.CreatedAt,
                                       PaidAt = p.PaidAt,
                                       FailedAt = p.FailedAt,
                                       FailureReason = p.FailureReason,
                                       LoyaltyPointsUsed = p.LoyaltyPointsUsed
                                   })
                                   .ToListAsync(cancellationToken);

            return Result<PagedResult<PaymentDto>>.Success(new PagedResult<PaymentDto>
            {
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                Items = items
            });
        }
    }
}
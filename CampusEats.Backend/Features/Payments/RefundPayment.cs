using CampusEats.Backend.Common;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace CampusEats.Backend.Features.Payments;

public static class RefundPayment
{
    public record Command(Guid PaymentId, string? Reason) : IRequest<Result<PaymentDto>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PaymentId).NotEmpty();
        }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<PaymentDto>>
    {
        private readonly AppDbContext _context;
        private readonly StripeClient _stripeClient;

        public Handler(AppDbContext context, StripeClient stripeClient)
        {
            _context = context;
            _stripeClient = stripeClient;
        }

        public async Task<Result<PaymentDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

            if (payment == null)
                return Result<PaymentDto>.Failure("Payment not found");
            if (payment.Status != PaymentStatus.Completed)
                return Result<PaymentDto>.Failure("Cannot refund payment that is not completed");
            if (payment.Method != PaymentMethod.Card)
                return Result<PaymentDto>.Failure("Only Stripe card payments can be refunded");

            // Stripe refund
            try
            {
                var refundOptions = new Stripe.RefundCreateOptions
                {
                    PaymentIntent = payment.StripePaymentIntentId,
                    Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : "requested_by_customer"
                };
                var refundService = new Stripe.RefundService(_stripeClient);
                var refund = await refundService.CreateAsync(refundOptions, cancellationToken: cancellationToken);

                // Update local payment status
                payment.Status = PaymentStatus.Refunded;
                payment.FailedAt = DateTime.UtcNow;
                payment.FailureReason = request.Reason ?? "Refunded via API";

                await _context.SaveChangesAsync(cancellationToken);

                return Result<PaymentDto>.Success(new PaymentDto
                {
                    Id = payment.Id,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    Status = payment.Status,
                    Method = payment.Method,
                    StripePaymentIntentId = payment.StripePaymentIntentId,
                    StripeSessionId = payment.StripeSessionId,
                    CreatedAt = payment.CreatedAt,
                    PaidAt = payment.PaidAt,
                    FailedAt = payment.FailedAt,
                    FailureReason = payment.FailureReason,
                    LoyaltyPointsUsed = payment.LoyaltyPointsUsed
                });
            }
            catch (StripeException ex)
            {
                return Result<PaymentDto>.Failure("Stripe refund failed: " + ex.Message);
            }
        }
    }
}
using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace CampusEats.Backend.Features.Payments;

public static class ProcessPayment
{
    public record Command : IRequest<Result<PaymentDto>>
    {
        public Guid OrderId { get; init; }
        public string PaymentMethod { get; init; } = string.Empty;      // string pentru API
        public decimal Amount { get; init; }
        public decimal? LoyaltyPointsUsed { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.PaymentMethod).NotEmpty().Must(m => PaymentMethodExists(m))
                .WithMessage("Payment method must be 'Card', 'Cash', or 'LoyaltyPoints'");
            RuleFor(x => x.Amount).GreaterThan(0m);
        }

        private bool PaymentMethodExists(string value) =>
            Enum.TryParse<PaymentMethod>(value, true, out var _);
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
            var order = await _context.Orders.Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order is null)
                return Result<PaymentDto>.Failure("Order not found");
            // 1. Calculate how much has been paid so far
            var existingPayments = await _context.Payments
                .Where(p => p.OrderId == order.Id && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Pending)) // Pending card payments count towards "attempted" but careful with double charging. 
                                                                                                                                  // For simplicity/safety, let's only count Completed for determining what's left, 
                                                                                                                                  // BUT if there's a Pending Stripe session, we might want to be careful. 
                                                                                                                                  // For now, let's sum Completed to see what's truly paid.
                .Where(p => p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount, cancellationToken);

            var remainingAmount = order.TotalAmount - existingPayments;

            // 2. Validate Amount
            if (request.Amount <= 0)
                return Result<PaymentDto>.Failure("Amount must be greater than 0");

            if (request.Amount > remainingAmount + 0.01m) // Small grace for rounding
                return Result<PaymentDto>.Failure($"Amount ({request.Amount}) exceeds remaining balance ({remainingAmount})");

            if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method))
                return Result<PaymentDto>.Failure("Invalid payment method");

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = request.Amount,
                Method = method,
                CreatedAt = DateTime.UtcNow,
                Status = PaymentStatus.Pending
            };

            if (method == PaymentMethod.Card)
            {
                // Creăm Stripe Checkout Session (nu PaymentIntent)
                var sessionOptions = new Stripe.Checkout.SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    Mode = "payment",
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                    {
                        new Stripe.Checkout.SessionLineItemOptions
                        {
                            PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                            {
                                Currency = "ron",
                                ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"CampusEats Order #{order.OrderNumber}",
                                    Description = $"Order ID: {order.Id} (Partial/Full Payment)"
                                },
                                UnitAmount = (long)(request.Amount * 100)
                            },
                            Quantity = 1
                        }
                    },
                    SuccessUrl = $"http://localhost:5002/checkout/success?session_id={{CHECKOUT_SESSION_ID}}&order_id={order.Id}",
                    CancelUrl = $"http://localhost:5002/checkout/cancel?order_id={order.Id}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["OrderId"] = order.Id.ToString(),
                        ["UserId"] = order.UserId.ToString()
                    }
                };

                var sessionService = new Stripe.Checkout.SessionService(_stripeClient);

                try
                {
                    var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);
                    
                    payment.StripeSessionId = session.Id;
                    payment.Status = PaymentStatus.Pending;
                    // Order PaymentStatus rămâne cum era sau devine Partial dacă nu e full? 
                    // Dacă e doar Card (full), e Pending. Dacă e split, tot Pending e partea de card.
                    
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync(cancellationToken);

                    return Result<PaymentDto>.Success(new PaymentDto
                    {
                        Id = payment.Id,
                        OrderId = payment.OrderId,
                        Amount = payment.Amount,
                        Status = payment.Status,
                        Method = payment.Method,
                        StripeSessionId = payment.StripeSessionId,
                        StripeClientSecret = session.Url, // URL-ul de redirect
                        CreatedAt = payment.CreatedAt
                    });
                }
                catch (StripeException ex)
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.FailureReason = ex.StripeError?.Message ?? ex.Message;
                    payment.FailedAt = DateTime.UtcNow;
                    // Nu marcam comanda Failed decat daca e totul failed? 
                    // Lasam Order PaymentStatus neatins aici momentan.
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync(cancellationToken);
                    return Result<PaymentDto>.Failure("Stripe payment failed: " + payment.FailureReason);
                }
            }
            else if (method == PaymentMethod.Cash)
            {
                payment.Status = PaymentStatus.Pending;
                // La Cash, plata nu e Completed, e Pending pana o incaseaza livratorul.
                // Dar daca avem Loyalty (Completed) + Cash (Pending), statusul comenzii e Partial?
                // Sa zicem ca e "Pending" ca status general daca mai e ceva de plata (fizic sau online).
            }
            else if (method == PaymentMethod.LoyaltyPoints)
            {
                var pointsNeeded = request.LoyaltyPointsUsed ?? 0m;
                if (order.User.LoyaltyPoints < pointsNeeded)
                    return Result<PaymentDto>.Failure("Insufficient loyalty points");

                order.User.LoyaltyPoints -= pointsNeeded;
                payment.Status = PaymentStatus.Completed;
                payment.PaidAt = DateTime.UtcNow;
                payment.LoyaltyPointsUsed = pointsNeeded;
                
                // Loyalty e instant paid.
            }
            else
            {
                return Result<PaymentDto>.Failure("Invalid payment method");
            }

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken); // Save payment first

            // 3. Update Order Payment Status based on TOTAL processed
            var totalPaidOrPending = existingPayments + payment.Amount; 
            // Daca metoda e Cash, payment e Pending. Daca e Card, e Pending. Daca e Loyalty, e Completed.
            // Vrem sa stim daca am acoperit 'teoretic' totul.
            
            // Recalculate totals including this new payment (whether pending or completed)
            // We assume if Card session created -> user will pay.
            // If Cash selected -> user will pay.
            // So if (Existing + Current Request Amount) >= Total -> consider "Paid" (if loyalty only) or "Pending" (if has cash/card)
            
            var amountCovered = existingPayments;
            if (payment.Status == PaymentStatus.Completed) amountCovered += payment.Amount;
            
            // Check if fully paid NOW (e.g. 100% Loyalty)
            if (amountCovered >= order.TotalAmount - 0.01m)
            {
                order.PaymentStatus = "Paid";
            }
            else
            {
                // Not fully paid yet (or waiting for external ACTION like Cash/Card)
                // If we secured the INTENT to pay the rest (via this Cash or Card request), 
                // and (Existing + Request) >= Total, then we are "good" to proceed with order logic generally, 
                // but the status strictly depends on money received.
                
                if (existingPayments > 0)
                    order.PaymentStatus = "Partial"; // Some part (Loyalty) was paid, rest is pending.
                else 
                    order.PaymentStatus = "Pending";
            }

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
                StripeClientSecret = null,
                CreatedAt = payment.CreatedAt,
                PaidAt = payment.PaidAt,
                FailedAt = payment.FailedAt,
                FailureReason = payment.FailureReason,
                LoyaltyPointsUsed = payment.LoyaltyPointsUsed
            });
        }
    }
}
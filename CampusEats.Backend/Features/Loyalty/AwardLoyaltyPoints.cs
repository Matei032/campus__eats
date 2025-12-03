using CampusEats.Backend.Common;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Loyalty;

public static class AwardLoyaltyPoints
{
    public record Command(Guid UserId, int Points, string Description, Guid? OrderId = null) : IRequest<Result>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required");
            RuleFor(x => x.Points).GreaterThan(0).WithMessage("Points must be greater than 0");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required");
        }
    }

    public class Handler : IRequestHandler<Command, Result>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                return Result.Failure("User not found");

            user.LoyaltyPoints += request.Points;
            user.UpdatedAt = DateTime.UtcNow;

            var transaction = new LoyaltyTransaction
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PointsChange = request.Points,
                Type = LoyaltyTransactionType.Earned,
                Description = request.Description,
                OrderId = request.OrderId,
                CreatedAt = DateTime.UtcNow
            };

            _context.LoyaltyTransactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
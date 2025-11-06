using CampusEats.Backend.Common;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Menu;

public static class DeleteProduct
{
    // Command
    public record Command(Guid Id) : IRequest<Result>;

    // Validator
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Product ID is required");
        }
    }

    // Handler
    internal sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product is null)
            {
                return Result.Failure($"Product with ID {request.Id} not found");
            }

            // Check if product is used in any orders
            var hasOrders = await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == request.Id, cancellationToken);

            if (hasOrders)
            {
                return Result.Failure("Cannot delete product that has been ordered. Consider marking it as unavailable instead.");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
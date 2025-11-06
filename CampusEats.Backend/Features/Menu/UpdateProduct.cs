using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs; // ✅ ADD THIS
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Menu;

public static class UpdateProduct
{
    public record Command : IRequest<Result<ProductDto>>
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public string Category { get; init; } = string.Empty;
        public string? ImageUrl { get; init; }
        public List<string> Allergens { get; init; } = new();
        public string? DietaryRestrictions { get; init; }
        public bool IsAvailable { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Product ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(100).WithMessage("Product name cannot exceed 100 characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0")
                .LessThanOrEqualTo(1000).WithMessage("Price cannot exceed 1000 RON");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required")
                .Must(category => new[] { "Main", "Drink", "Dessert", "Snack" }.Contains(category))
                .WithMessage("Category must be one of: Main, Drink, Dessert, Snack");
        }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<ProductDto>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<ProductDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product is null)
            {
                return Result<ProductDto>.Failure($"Product with ID {request.Id} not found");
            }

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.Category = request.Category;
            product.ImageUrl = request.ImageUrl;
            product.Allergens = request.Allergens;
            product.DietaryRestrictions = request.DietaryRestrictions;
            product.IsAvailable = request.IsAvailable;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                ImageUrl = product.ImageUrl,
                Allergens = product.Allergens,
                DietaryRestrictions = product.DietaryRestrictions,
                IsAvailable = product.IsAvailable,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return Result<ProductDto>.Success(dto);
        }
    }
}
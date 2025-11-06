using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Menu;

public static class GetMenu
{
    public record ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> Allergens { get; set; }
    }

    public record Query : IRequest<List<ProductDto>>;

    internal sealed class Handler : IRequestHandler<Query, List<ProductDto>>
    {
        private readonly AppDbContext _dbContext;

        public Handler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ProductDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var products = await _dbContext.Products
                .AsNoTracking()
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Category = p.Category,
                    ImageUrl = p.ImageUrl,
                    Allergens = p.Allergens
                })
                .ToListAsync(cancellationToken);

            return products;
        }
    }
}
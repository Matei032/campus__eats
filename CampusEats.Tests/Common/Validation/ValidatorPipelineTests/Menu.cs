using CampusEats.Backend.Common;
using CampusEats.Backend.Common.Behaviors;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Features.Menu;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace CampusEats.Tests.Common.Validation.ValidatorPipelineTests;

public class Menu
{
    private static async Task<TResponse> RunBehavior<TRequest, TResponse>(
        TRequest request,
        IEnumerable<IValidator<TRequest>> validators,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken ct = default)
        where TRequest : IRequest<TResponse>
    {
        var behavior = new ValidationBehavior<TRequest, TResponse>(validators);

        // IMPORTANT: RequestHandlerDelegate<TResponse> primește CancellationToken
        RequestHandlerDelegate<TResponse> del = (CancellationToken token) => next(token);

        return await behavior.Handle(request, del, ct);
    }

    [Fact]
    public async Task CreateProductValidator_EmptyName_ReturnsFailure()
    {
        var cmd = new CreateProduct.Command
        {
            Name = "",
            Description = "Desc",
            Price = 10,
            Category = "Main"
        };

        var result = await RunBehavior<CreateProduct.Command, Result<ProductDto>>(
            cmd,
            new[] { new CreateProduct.Validator() },
            _ => Task.FromResult(Result<ProductDto>.Success(new ProductDto())));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Product name is required"));
    }

    [Fact]
    public async Task CreateProductValidator_InvalidCategory_ReturnsFailure()
    {
        var cmd = new CreateProduct.Command
        {
            Name = "Burger",
            Description = "Desc",
            Price = 10,
            Category = "InvalidCategory"
        };

        var result = await RunBehavior<CreateProduct.Command, Result<ProductDto>>(
            cmd,
            new[] { new CreateProduct.Validator() },
            _ => Task.FromResult(Result<ProductDto>.Success(new ProductDto())));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Category must be one of"));
    }

    [Fact]
    public async Task UpdateProductValidator_EmptyId_ReturnsFailure()
    {
        var cmd = new UpdateProduct.Command
        {
            Id = Guid.Empty,
            Name = "Burger",
            Description = "Desc",
            Price = 10,
            Category = "Main",
            IsAvailable = true
        };

        var result = await RunBehavior<UpdateProduct.Command, Result<ProductDto>>(
            cmd,
            new[] { new UpdateProduct.Validator() },
            _ => Task.FromResult(Result<ProductDto>.Success(new ProductDto())));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Product ID is required"));
    }

    [Fact]
    public async Task UpdateProductValidator_ValidRequest_AllowsNext()
    {
        var cmd = new UpdateProduct.Command
        {
            Id = Guid.NewGuid(),
            Name = "Burger",
            Description = "Desc",
            Price = 10,
            Category = "Main",
            IsAvailable = true,
            Allergens = new List<string>()
        };

        var result = await RunBehavior<UpdateProduct.Command, Result<ProductDto>>(
            cmd,
            new[] { new UpdateProduct.Validator() },
            _ => Task.FromResult(Result<ProductDto>.Success(new ProductDto { Name = cmd.Name, Category = cmd.Category })));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Burger");
        result.Value.Category.Should().Be("Main");
    }
}
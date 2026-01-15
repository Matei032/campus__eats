using System.Threading;
using System.Threading.Tasks;
using CampusEats.Backend.Common;
using CampusEats.Backend.Common.Behaviors;
using FluentValidation;
using MediatR;
using FluentAssertions;
using Xunit;

namespace CampusEats.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public record TestRequest : IRequest<Result<string>>
    {
        public string Value { get; init; } = string.Empty;
    }

    public class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Value is required")
                .MinimumLength(3).WithMessage("Value must be at least 3 characters");
        }
    }

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var validators = Array.Empty<IValidator<TestRequest>>();

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (CancellationToken _) =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("success"));
        };

        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("success");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        // Arrange
        var request = new TestRequest { Value = "valid" };
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (CancellationToken _) =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("success"));
        };

        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("success");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var request = new TestRequest { Value = "" }; // Empty value
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (CancellationToken _) =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("success"));
        };

        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Value is required");
        nextCalled.Should().BeFalse(); // Next should NOT be called
    }

    [Fact]
    public async Task Handle_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var request = new TestRequest { Value = "ab" }; // Too short
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (CancellationToken _) =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("success"));
        };

        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Value must be at least 3 characters");
        nextCalled.Should().BeFalse();
    }
}
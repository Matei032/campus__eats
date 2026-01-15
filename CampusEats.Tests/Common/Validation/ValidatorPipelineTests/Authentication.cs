using CampusEats.Backend.Common;
using CampusEats.Backend.Common.Behaviors;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Features.Authentication;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace CampusEats.Tests.Common.Validation.ValidatorPipelineTests;

public class Authentication
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
    public async Task RegisterValidator_InvalidEmail_ReturnsFailure()
    {
        var cmd = new Register.Command
        {
            Email = "not-an-email",
            Password = "TestPass123!",
            FullName = "Test User",
            PhoneNumber = "+40712345678",
            StudentId = "S1"
        };

        var result = await RunBehavior<Register.Command, Result<UserDto>>(
            cmd,
            new[] { new Register.Validator() },
            _ => Task.FromResult(Result<UserDto>.Success(new UserDto())));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid email format"));
    }

    [Fact]
    public async Task RegisterValidator_WeakPassword_ReturnsFailure()
    {
        var cmd = new Register.Command
        {
            Email = "user@test.com",
            Password = "weakpass",
            FullName = "Test User",
            PhoneNumber = "+40712345678"
        };

        var result = await RunBehavior<Register.Command, Result<UserDto>>(
            cmd,
            new[] { new Register.Validator() },
            _ => Task.FromResult(Result<UserDto>.Success(new UserDto())));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Password must contain", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisterValidator_ValidRequest_AllowsNext()
    {
        var cmd = new Register.Command
        {
            Email = "user@test.com",
            Password = "StrongPass123!",
            FullName = "Test User",
            PhoneNumber = "+40712345678",
            StudentId = "S1"
        };

        var result = await RunBehavior<Register.Command, Result<UserDto>>(
            cmd,
            new[] { new Register.Validator() },
            _ => Task.FromResult(Result<UserDto>.Success(new UserDto { Email = cmd.Email })));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task LoginValidator_EmptyEmail_ReturnsFailure()
    {
        var query = new Login.Query
        {
            Email = "",
            Password = "AnyPass123!"
        };

        var result = await RunBehavior<Login.Query, Result<AuthResponseDto>>(
            query,
            new[] { new Login.Validator() },
            _ => Task.FromResult(Result<AuthResponseDto>.Success(new AuthResponseDto { Token = "x", User = new UserDto() })));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Email is required"));
    }

    [Fact]
    public async Task LoginValidator_ValidRequest_AllowsNext()
    {
        var query = new Login.Query
        {
            Email = "user@test.com",
            Password = "AnyPass123!"
        };

        var result = await RunBehavior<Login.Query, Result<AuthResponseDto>>(
            query,
            new[] { new Login.Validator() },
            _ => Task.FromResult(Result<AuthResponseDto>.Success(new AuthResponseDto { Token = "ok", User = new UserDto() })));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("ok");
    }
}
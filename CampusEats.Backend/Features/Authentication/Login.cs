using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Common.Services;
using CampusEats.Backend.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Authentication;

public static class Login
{
    // QUERY
    public record Query : IRequest<Result<AuthResponseDto>>
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }
    
    // VALIDATOR
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
    
    // HANDLER
    internal sealed class Handler : IRequestHandler<Query, Result<AuthResponseDto>>
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public Handler(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<Result<AuthResponseDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            // 1. Find user by email
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user is null)
            {
                return Result<AuthResponseDto>.Failure("Invalid email or password");
            }

            // 2. Verify password
            var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!passwordValid)
            {
                return Result<AuthResponseDto>.Failure("Invalid email or password");
            }

            // 3. Check if user is active
            if (!user.IsActive)
            {
                return Result<AuthResponseDto>.Failure("Account is deactivated. Please contact support.");
            }

            // 4. Generate JWT token
            var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role, user.FullName);

            // 5. Build response
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                StudentId = user.StudentId,
                LoyaltyPoints = user.LoyaltyPoints,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            var response = new AuthResponseDto
            {
                Token = token,
                User = userDto
            };

            return Result<AuthResponseDto>.Success(response);
        }
    }
}
using System.Security.Claims;
using CampusEats.Backend.Common;
using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Features.Authentication;

public static class GetCurrentUser
{
    // QUERY
    public record Query : IRequest<Result<UserDto>>
    {
        public Guid UserId { get; init; }
    }
    
    // HANDLER
    internal sealed class Handler : IRequestHandler<Query, Result<UserDto>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<UserDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user is null)
            {
                return Result<UserDto>.Failure("User not found");
            }

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

            return Result<UserDto>.Success(userDto);
        }
    }
}
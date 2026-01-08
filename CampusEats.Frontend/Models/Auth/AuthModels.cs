using System;

namespace CampusEats.Frontend.Models.Auth
{
    // Corespunde cu LoginDto din Backend
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Corespunde cu RegisterDto din Backend
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? StudentId { get; set; }
    }

    // Corespunde cu AuthResponseDto din Backend
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    // Corespunde cu UserDto din Backend
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public decimal LoyaltyPoints { get; set; }
        public bool IsActive { get; set; }
    }
}
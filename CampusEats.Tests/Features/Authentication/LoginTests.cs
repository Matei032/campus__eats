using CampusEats.Backend.Common.Services;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Authentication;
using CampusEats.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CampusEats.Tests.Features.Authentication;

public class LoginTests
{
    private readonly IConfiguration _configuration;

    public LoginTests()
    {
        // Setup configuration for JWT
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Jwt:SecretKey", "CampusEats_2025_SuperSecretJWT_KeyForProductionUse_Min64Chars!@#$%^&*()_+"},
            {"Jwt:Issuer", "CampusEatsAPI"},
            {"Jwt:Audience", "CampusEatsClients"},
            {"Jwt:ExpirationDays", "7"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var jwtService = new JwtService(_configuration);
        var handler = new Login.Handler(context, jwtService);

        // Create test user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "testuser@campus.ro",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            LoyaltyPoints = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var query = new Login.Query
        {
            Email = "testuser@campus.ro",
            Password = "TestPass123!"
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().NotBeNullOrEmpty();
        result.Value.User.Should().NotBeNull();
        result.Value.User.Email.Should().Be("testuser@campus.ro");
        result.Value.User.FullName.Should().Be("Test User");
        result.Value.User.Role.Should().Be("Student");
        result.Value.User.LoyaltyPoints.Should().Be(100);
        
        // Verify token is valid JWT format (starts with eyJ)
        result.Value.Token.Should().StartWith("eyJ");
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var jwtService = new JwtService(_configuration);
        var handler = new Login.Handler(context, jwtService);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "testuser@campus.ro",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass123!"),
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var query = new Login.Query
        {
            Email = "testuser@campus.ro",
            Password = "WrongPass123!" // Wrong password
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid email or password"));
    }

    [Fact]
    public async Task Handle_NonExistentEmail_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var jwtService = new JwtService(_configuration);
        var handler = new Login.Handler(context, jwtService);

        var query = new Login.Query
        {
            Email = "nonexistent@campus.ro",
            Password = "SomePass123!"
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid email or password"));
    }

    [Fact]
    public async Task Handle_InactiveAccount_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var jwtService = new JwtService(_configuration);
        var handler = new Login.Handler(context, jwtService);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactive@campus.ro",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            FullName = "Inactive User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = false, // Inactive account
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var query = new Login.Query
        {
            Email = "inactive@campus.ro",
            Password = "TestPass123!"
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Account is deactivated"));
    }
}
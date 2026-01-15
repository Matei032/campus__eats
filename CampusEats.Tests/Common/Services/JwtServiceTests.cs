using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CampusEats.Backend.Common.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CampusEats.Tests.Common.Services;

public class JwtServiceTests
{
    private static IConfiguration BuildConfig(
        string? secret = null,
        string issuer = "CampusEatsAPI",
        string audience = "CampusEatsClients",
        string expirationDays = "7")
    {
        secret ??= "CampusEats_2025_SuperSecretJWT_KeyForProductionUse_Min64Chars!@#$%^&*()_+";

        var settings = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = secret,
            ["Jwt:Issuer"] = issuer,
            ["Jwt:Audience"] = audience,
            ["Jwt:ExpirationDays"] = expirationDays
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings!)
            .Build();
    }

    [Fact]
    public void GenerateToken_ContainsExpectedClaims_AndValidateTokenReturnsPrincipal()
    {
        // Arrange
        var config = BuildConfig(expirationDays: "7");
        var jwtService = new JwtService(config);

        var userId = Guid.NewGuid();
        var email = "user@test.com";
        var role = "Student";
        var fullName = "Test User";

        // Act
        var token = jwtService.GenerateToken(userId, email, role, fullName);

        // Assert claims by reading JWT (most reliable)
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be("CampusEatsAPI");
        jwt.Audiences.Should().Contain("CampusEatsClients");

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == fullName);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);

        // ValidateToken: should return principal for valid token
        var principal = jwtService.ValidateToken(token);
        principal.Should().NotBeNull();

        // sanity: token structure
        token.Split('.').Length.Should().Be(3);
    }

    [Fact]
    public void ValidateToken_ReturnsNull_WhenAudienceIsInvalid()
    {
        var validConfig = BuildConfig(audience: "AudienceA");
        var jwtService = new JwtService(validConfig);

        var token = jwtService.GenerateToken(Guid.NewGuid(), "x@y.com", "Student", "X");

        var invalidAudienceConfig = BuildConfig(audience: "AudienceB");
        var jwtServiceInvalidAudience = new JwtService(invalidAudienceConfig);

        jwtServiceInvalidAudience.ValidateToken(token).Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ReturnsNull_WhenIssuerIsInvalid()
    {
        var validConfig = BuildConfig(issuer: "IssuerA");
        var jwtService = new JwtService(validConfig);

        var token = jwtService.GenerateToken(Guid.NewGuid(), "x@y.com", "Student", "X");

        var invalidIssuerConfig = BuildConfig(issuer: "IssuerB");
        var jwtServiceInvalidIssuer = new JwtService(invalidIssuerConfig);

        jwtServiceInvalidIssuer.ValidateToken(token).Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ReturnsNull_WhenTokenIsExpired()
    {
        var config = BuildConfig(expirationDays: "0");
        var jwtService = new JwtService(config);

        var token = jwtService.GenerateToken(Guid.NewGuid(), "x@y.com", "Student", "X");

        Thread.Sleep(1100);

        jwtService.ValidateToken(token).Should().BeNull();
    }

    [Fact]
    public void GenerateToken_SetsExpiresAccordingToExpirationDays()
    {
        var config = BuildConfig(expirationDays: "1");
        var jwtService = new JwtService(config);

        var token = jwtService.GenerateToken(Guid.NewGuid(), "x@y.com", "Student", "X");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow.AddHours(23));
        jwt.ValidTo.Should().BeBefore(DateTime.UtcNow.AddHours(25));
    }
}
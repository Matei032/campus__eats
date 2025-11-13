using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Authentication;
using CampusEats.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests.Features.Authentication;

public class RegisterTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithUser()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new Register.Handler(context);

        var command = new Register.Command
        {
            Email = "newuser@campus.ro",
            Password = "SecurePass123!",
            FullName = "New User",
            PhoneNumber = "+40712345678",
            StudentId = "STU999"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be("newuser@campus.ro");
        result.Value.FullName.Should().Be("New User");
        result.Value.Role.Should().Be("Student");
        result.Value.PhoneNumber.Should().Be("+40712345678");
        result.Value.StudentId.Should().Be("STU999");
        result.Value.LoyaltyPoints.Should().Be(0);
        result.Value.IsActive.Should().BeTrue();

        // Verify user was saved to database
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@campus.ro");
        savedUser.Should().NotBeNull();
        savedUser!.PasswordHash.Should().NotBeEmpty();
        
        // Verify password was hashed (not plain text)
        savedUser.PasswordHash.Should().NotBe("SecurePass123!");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new Register.Handler(context);

        // Create existing user
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@campus.ro",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FullName = "Existing User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var command = new Register.Command
        {
            Email = "existing@campus.ro", // Same email
            Password = "NewPass123!",
            FullName = "New User",
            PhoneNumber = "+40723456789"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Email is already registered"));
    }

    [Fact]
    public async Task Handle_InvalidEmailFormat_FailsValidation()
    {
        // Arrange
        var validator = new Register.Validator();

        var command = new Register.Command
        {
            Email = "invalid-email", // Invalid format
            Password = "SecurePass123!",
            FullName = "Test User",
            PhoneNumber = "+40712345678"
        };

        // Act
        var validationResult = await validator.ValidateAsync(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage.Contains("Invalid email format"));
    }

    [Fact]
    public async Task Handle_WeakPassword_FailsValidation()
    {
        // Arrange
        var validator = new Register.Validator();

        var testCases = new[]
        {
            new { Password = "short", Error = "at least 8 characters" },
            new { Password = "nouppercase123!", Error = "uppercase letter" },
            new { Password = "NOLOWERCASE123!", Error = "lowercase letter" },
            new { Password = "NoNumbers!", Error = "number" },
            new { Password = "NoSpecial123", Error = "special character" }
        };

        foreach (var testCase in testCases)
        {
            var command = new Register.Command
            {
                Email = "test@campus.ro",
                Password = testCase.Password,
                FullName = "Test User",
                PhoneNumber = "+40712345678"
            };

            // Act
            var validationResult = await validator.ValidateAsync(command);

            // Assert
            validationResult.IsValid.Should().BeFalse($"Password '{testCase.Password}' should fail validation");
            validationResult.Errors.Should().Contain(e => e.ErrorMessage.Contains(testCase.Error),
                $"Expected error about '{testCase.Error}' for password '{testCase.Password}'");
        }
    }

    [Fact]
    public async Task Handle_MissingRequiredFields_FailsValidation()
    {
        // Arrange
        var validator = new Register.Validator();

        var command = new Register.Command
        {
            Email = "", // Missing
            Password = "", // Missing
            FullName = "", // Missing
            PhoneNumber = "" // Missing
        };

        // Act
        var validationResult = await validator.ValidateAsync(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().HaveCountGreaterOrEqualTo(4); // At least 4 required field errors
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Email");
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Password");
        validationResult.Errors.Should().Contain(e => e.PropertyName == "FullName");
        validationResult.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }
}
using AwesomeAssertions;
using RiskScreening.API.Modules.IAM.Application.Authentication;
using RiskScreening.API.Modules.IAM.Domain.Model.Commands;
using RiskScreening.UnitTests.Modules.IAM.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.IAM.Application;

/// <summary>
///     Unit tests for <see cref="SignInCommandValidator"/>.
///     Validates that FluentValidation rules are correctly defined —
///     no mocks or I/O needed.
/// </summary>
public class SignInCommandValidatorTests
{
    private readonly SignInCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = SignInCommandMother.WithNonExistentUser();
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyEmail_FailsValidation(string? email)
    {
        var result = _validator.Validate(new SignInCommand(email!, SignInCommandMother.ValidPassword()));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SignInCommand.Email));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void Validate_InvalidEmailFormat_FailsValidation(string email)
    {
        var result = _validator.Validate(new SignInCommand(email, SignInCommandMother.ValidPassword()));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SignInCommand.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyPassword_FailsValidation(string? password)
    {
        var result = _validator.Validate(new SignInCommand(SignInCommandMother.WithNonExistentUser().Email, password!));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SignInCommand.Password));
    }
}
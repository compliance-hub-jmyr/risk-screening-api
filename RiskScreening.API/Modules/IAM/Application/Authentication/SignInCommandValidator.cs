using FluentValidation;
using RiskScreening.API.Modules.IAM.Domain.Model.Commands;

namespace RiskScreening.API.Modules.IAM.Application.Authentication;

/// <summary>
/// Validates the <see cref="SignInCommand"/> before it is handled.
/// </summary>
/// <remarks>
/// Registered automatically by the FluentValidation pipeline behavior.
/// Validation runs prior to the handler, so the handler can assume inputs are well-formed.
/// </remarks>
public class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    /// <summary>
    /// Configures the validation rules for <see cref="SignInCommand"/>.
    /// </summary>
    /// <remarks>
    /// Rules:
    /// <list type="bullet">
    ///   <item><description><c>Email</c>: required and must be a valid email address format.</description></item>
    ///   <item><description><c>Password</c>: required, must not be empty.</description></item>
    /// </list>
    /// </remarks>
    public SignInCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
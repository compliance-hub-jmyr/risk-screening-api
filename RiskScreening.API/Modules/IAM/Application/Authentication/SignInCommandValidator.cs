using FluentValidation;
using RiskScreening.API.Modules.IAM.Domain.Model.Commands;

namespace RiskScreening.API.Modules.IAM.Application.Authentication;

public class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    public SignInCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
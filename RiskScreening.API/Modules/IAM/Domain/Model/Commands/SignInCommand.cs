using MediatR;
using RiskScreening.API.Modules.IAM.Application.Results;

namespace RiskScreening.API.Modules.IAM.Domain.Model.Commands;

public record SignInCommand(string Email, string Password)
    : IRequest<SignInResult>;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;

namespace RiskScreening.API.Modules.IAM.Application.Results;

/// <summary>
///     Application-level result for a successful sign-in.
///     Carries the authenticated user and the generated JWT token.
///     The controller maps this to <c>AuthenticatedUserResponse</c>.
/// </summary>
/// <param name="User"></param>
/// <param name="Token"></param>
/// <returns></returns>
public record SignInResult(User User, string Token);
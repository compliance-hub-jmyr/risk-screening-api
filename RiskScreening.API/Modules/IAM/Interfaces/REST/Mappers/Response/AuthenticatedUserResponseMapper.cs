using RiskScreening.API.Modules.IAM.Application.Results;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Resources.Responses;

namespace RiskScreening.API.Modules.IAM.Interfaces.REST.Mappers.Response;

/// <summary>
/// Maps authentication-related domain objects to <see cref="AuthenticatedUserResponse"/> DTOs.
/// </summary>
public static class AuthenticatedUserResponseMapper
{
    /// <summary>Maps a <see cref="SignInResult"/> (sign-in flow).</summary>
    public static AuthenticatedUserResponse ToResponse(SignInResult result)
    {
        return new AuthenticatedUserResponse(result.User.Id,
            result.User.Email.Value,
            result.User.Username.Value,
            result.User.Status.ToString(),
            result.User.Roles.Select(r => r.Name).ToList(),
            result.Token);
    }

    /// <summary>Maps a <see cref="User"/> aggregate + an existing token (current-user flow).</summary>
    public static AuthenticatedUserResponse ToResponse(User user, string token)
    {
        return new AuthenticatedUserResponse(user.Id,
            user.Email.Value,
            user.Username.Value,
            user.Status.ToString(),
            user.Roles.Select(r => r.Name).ToList(),
            token);
    }
}
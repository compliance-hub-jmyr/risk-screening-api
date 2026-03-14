using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;

namespace RiskScreening.API.Modules.IAM.Application.Ports;

/// <summary>
///     Port for JWT token generation and validation.
///     Implemented in Infrastructure using System.IdentityModel.Tokens.Jwt.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the given user.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    string GenerateToken(User user);

    /// <summary>
    /// Extracts the email claim from the given JWT token. Returns null if the token is invalid or does not contain an email claim.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    string? GetEmailFromToken(string token);
    
    /// <summary>
    /// Validates the given JWT token.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    bool ValidateToken(string token);
}
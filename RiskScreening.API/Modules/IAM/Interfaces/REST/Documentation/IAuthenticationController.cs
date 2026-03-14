using Microsoft.AspNetCore.Mvc;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Resources.Requests;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Resources.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace RiskScreening.API.Modules.IAM.Interfaces.REST.Documentation;

/// <summary>
/// OpenAPI contract for authentication endpoints.
/// <para>
/// Separates the documentation concern from the implementation.
/// Implementations live in <see cref="Controllers.AuthenticationController"/>.
/// </para>
/// </summary>
public interface IAuthenticationController
{

    /// <summary>Authenticate with email and password and receive a JWT token.</summary>
    [SwaggerOperation(
        Summary = "Sign in",
        Description =
            "Authenticate with email and password. Returns a JWT bearer token valid for the configured expiry period.",
        Tags = ["Authentication"])]
    [SwaggerResponse(StatusCodes.Status200OK, "Authenticated successfully.", typeof(AuthenticatedUserResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error — missing or invalid fields.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid credentials or account locked/suspended.")]
    Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken ct);
}
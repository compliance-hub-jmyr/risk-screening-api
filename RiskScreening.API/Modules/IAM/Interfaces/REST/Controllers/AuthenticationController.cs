using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Documentation;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Mappers.Request;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Mappers.Response;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Resources.Requests;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Resources.Responses;
using RiskScreening.API.Shared.Infrastructure.Configuration;
using RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Annotations;

namespace RiskScreening.API.Modules.IAM.Interfaces.REST.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiVersion(ApiVersioning.V1)]
[Produces("application/json")]
public class AuthenticationController(IMediator mediator)
    : ControllerBase, IAuthenticationController
{
    /// <inheritdoc/>
    [HttpPost("sign-in")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status200OK)]
    [ApiResponseBadRequest]
    [ApiResponseUnauthorized]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken ct)
    {
        var command = SignInRequestMapper.ToCommand(request);
        var result = await mediator.Send(command, ct);
        var response = AuthenticatedUserResponseMapper.ToResponse(result);
        return Ok(response);
    }
}
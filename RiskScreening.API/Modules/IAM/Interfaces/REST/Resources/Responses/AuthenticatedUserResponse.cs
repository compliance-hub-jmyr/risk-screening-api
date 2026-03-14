using Swashbuckle.AspNetCore.Annotations;

namespace RiskScreening.API.Modules.IAM.Interfaces.REST.Resources.Responses;

public record AuthenticatedUserResponse(
    [property: SwaggerSchema(Description = "Unique identifier of the authenticated user (UUID).", Nullable = false)]
    string Id,
    [property: SwaggerSchema(Description = "Email address of the authenticated user.", Format = "email", Nullable = false)]
    string Email,
    [property: SwaggerSchema(Description = "Display name of the authenticated user.", Nullable = false)]
    string Username,
    [property: SwaggerSchema(Description = "Account status: Active, Suspended, or Locked.", Nullable = false)]
    string Status,
    [property: SwaggerSchema(Description = "List of role names assigned to the user.", Nullable = false)]
    List<string> Roles,
    [property: SwaggerSchema(Description = "JWT bearer token to include in the Authorization header.", Nullable = false)]
    string Token
);
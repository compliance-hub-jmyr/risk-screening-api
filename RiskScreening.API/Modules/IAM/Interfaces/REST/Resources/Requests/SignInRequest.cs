using Swashbuckle.AspNetCore.Annotations;

namespace RiskScreening.API.Modules.IAM.Interfaces.REST.Resources.Requests;

/// <summary>Request body for the sign-in endpoint.</summary>
public record SignInRequest(
    [property: SwaggerSchema(
        Description = "Registered email address of the user.",
        Format = "email",
        Nullable = false)]
    string Email,
    [property: SwaggerSchema(
        Description = "Account password.",
        Format = "password",
        Nullable = false)]
    string Password
);
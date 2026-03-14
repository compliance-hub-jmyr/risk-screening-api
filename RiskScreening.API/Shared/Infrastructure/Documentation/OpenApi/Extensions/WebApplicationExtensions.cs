namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Extensions;

/// <summary>
///     Extension methods for <see cref="WebApplication"/> to configure
///     the OpenAPI documentation middleware and CORS policy.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    ///     Registers OpenAPI documentation middleware.
    ///     <list type="bullet">
    ///         <item>Native OpenAPI JSON at <c>/openapi/v1.json</c></item>
    ///         <item>Swashbuckle JSON at <c>/swagger/v1/swagger.json</c></item>
    ///         <item>Swagger UI at <c>/swagger</c></item>
    ///     </list>
    /// </summary>
    /// <remarks>
    ///     Should only be called in non-production environments.
    ///     Wrap with <c>if (app.Environment.IsDevelopment())</c> or similar.
    /// </remarks>
    public static void UseOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    /// <summary>
    ///     Applies the <c>AllowAllPolicy</c> CORS policy registered by
    ///     <c>AddCorsPolicy</c>.
    /// </summary>
    public static void UseCorsPolicy(this WebApplication app)
    {
        app.UseCors("AllowAllPolicy");
    }
}
using Microsoft.OpenApi;
using RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Filters;
using RiskScreening.API.Shared.Interfaces.REST.Resources;

namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Extensions;

/// <summary>
///     Extension methods for <see cref="WebApplicationBuilder"/> to register
///     OpenAPI documentation and CORS services.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    ///     Registers OpenAPI documentation services (native + Swashbuckle).
    ///     <list type="bullet">
    ///         <item>JWT Bearer security scheme</item>
    ///         <item>Swashbuckle annotations support</item>
    ///         <item><see cref="StandardResponsesOperationFilter"/> for custom response attributes</item>
    ///         <item><see cref="ErrorResponse"/> schema registration</item>
    ///     </list>
    /// </summary>
    public static WebApplicationBuilder AddOpenApiDocumentation(this WebApplicationBuilder builder)
    {
        // Native ASP.NET Core OpenAPI — /openapi/v1.json
        builder.Services.AddOpenApi();

        // Required for Swashbuckle to discover controller endpoints
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            var info = builder.Configuration.GetSection("OpenApi");

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = info["Title"] ?? builder.Environment.ApplicationName,
                Version = info["Version"] ?? "v1",
                Description = info["Description"] ?? string.Empty,
                Contact = new OpenApiContact
                {
                    Name = info["Contact:Name"] ?? string.Empty,
                    Email = info["Contact:Email"] ?? string.Empty
                },
                License = new OpenApiLicense
                {
                    Name = "Apache 2.0",
                    Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
                }
            });

            // JWT Bearer authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description  = "Enter JWT token obtained from /api/authentication/sign-in",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });

            // In Microsoft.OpenApi 2.x, AddSecurityRequirement takes Func<OpenApiDocument, OpenApiSecurityRequirement>
            options.AddSecurityRequirement(doc =>
            {
                var requirement = new OpenApiSecurityRequirement();
                requirement.Add(new OpenApiSecuritySchemeReference("Bearer", doc), []);
                return requirement;
            });

            // Enable [SwaggerOperation], [SwaggerResponse], etc.
            options.EnableAnnotations();

            // Reads [ApiResponseBadRequest], [ApiResponsesStandard], etc. and adds responses
            options.OperationFilter<StandardResponsesOperationFilter>();

            // Registers ErrorResponse in /components/schemas so $ref links resolve in Swagger UI
            options.DocumentFilter<ErrorResponseDocumentFilter>();
        });

        return builder;
    }

    /// <summary>
    ///     Registers a permissive CORS policy named <c>AllowAllPolicy</c>.
    ///     Allows any origin, method, and header.
    /// </summary>
    /// <remarks>
    ///     This policy is intended for development and internal APIs only.
    ///     Restrict origins in production environments.
    /// </remarks>
    public static WebApplicationBuilder AddCorsPolicy(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAllPolicy",
                policy => policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        });

        return builder;
    }
}

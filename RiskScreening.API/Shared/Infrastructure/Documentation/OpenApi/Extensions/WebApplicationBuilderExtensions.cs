using Microsoft.OpenApi;
using RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Filters;
using RiskScreening.API.Shared.Interfaces.REST.Resources;
using Swashbuckle.AspNetCore.Annotations;

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
    ///         <item>API grouping by module (All, IAM, Suppliers)</item>
    ///     </list>
    /// </summary>
    public static void AddOpenApiDocumentation(this WebApplicationBuilder builder)
    {
        // Native ASP.NET Core OpenAPI — /openapi/v1.json
        builder.Services.AddOpenApi();

        // Required for Swashbuckle to discover controller endpoints
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            var info = builder.Configuration.GetSection("OpenApi");

            var contact = new OpenApiContact
            {
                Name = info["Contact:Name"] ?? string.Empty,
                Email = info["Contact:Email"] ?? string.Empty
            };

            var license = new OpenApiLicense
            {
                Name = "Apache 2.0",
                Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
            };

            // ── API Groups (one Swagger doc per module) ──────────────────────

            options.SwaggerDoc("all", new OpenApiInfo
            {
                Title = info["Title"] ?? builder.Environment.ApplicationName,
                Version = info["Version"] ?? "v1",
                Description = info["Description"] ?? string.Empty,
                Contact = contact,
                License = license
            });

            options.SwaggerDoc("iam", new OpenApiInfo
            {
                Title = "Risk Screening — IAM Module",
                Version = info["Version"] ?? "v1",
                Description = "Identity & Access Management: authentication, users and roles.",
                Contact = contact,
                License = license
            });

            options.SwaggerDoc("suppliers", new OpenApiInfo
            {
                Title = "Risk Screening — Suppliers Module",
                Version = info["Version"] ?? "v1",
                Description = "Supplier management: CRUD, due-diligence lifecycle, risk screening.",
                Contact = contact,
                License = license
            });

            // Route each endpoint to the correct group based on its Swagger tag
            options.DocInclusionPredicate((docName, apiDesc) =>
            {
                if (docName == "all") return true;

                var tags = apiDesc.ActionDescriptor.EndpointMetadata
                    .OfType<SwaggerOperationAttribute>()
                    .SelectMany(a => a.Tags ?? [])
                    .ToList();

                return docName switch
                {
                    "iam" => tags.Any(t =>
                        t.Equals("Authentication", StringComparison.OrdinalIgnoreCase)),
                    "suppliers" => tags.Any(t =>
                        t.Equals("Suppliers", StringComparison.OrdinalIgnoreCase)),
                    _ => false
                };
            });

            // ── Security ─────────────────────────────────────────────────────

            // JWT Bearer authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Enter JWT token obtained from /api/authentication/sign-in",
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

            // ── Filters & Annotations ────────────────────────────────────────

            // Enable [SwaggerOperation], [SwaggerResponse], etc.
            options.EnableAnnotations();

            // Reads [ApiResponseBadRequest], [ApiResponsesStandard], etc. and adds responses
            options.OperationFilter<StandardResponsesOperationFilter>();

            // Registers ErrorResponse in /components/schemas so $ref links resolve in Swagger UI
            options.DocumentFilter<ErrorResponseDocumentFilter>();

            // Injects realistic example values into request/response schemas
            options.SchemaFilter<SchemaExamplesFilter>();
        });
    }
}

using Microsoft.OpenApi;
using RiskScreening.API.Shared.Interfaces.REST.Resources;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Filters;

/// <summary>
///     Ensures the <see cref="ErrorResponse"/> schema is always present in
///     <c>/components/schemas</c>, even though no endpoint declares it via
///     <c>[ProducesResponseType]</c>.
/// <para>
///     Without this filter, <see cref="StandardResponsesOperationFilter"/> emits
///     <c>$ref: '#/components/schemas/ErrorResponse'</c> but Swashbuckle never
///     registers the schema automatically, causing Swagger UI resolver errors.
/// </para>
/// </summary>
public class ErrorResponseDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Generate the schema from the actual C# type so it stays in sync
        // with ErrorResponse automatically — no manual field maintenance.
        var schema = context.SchemaGenerator.GenerateSchema(
            typeof(ErrorResponse),
            context.SchemaRepository);

        // Only add if not already present (idempotent).
        if (swaggerDoc.Components is { Schemas: not null })
            swaggerDoc.Components.Schemas.TryAdd("ErrorResponse", schema);
    }
}

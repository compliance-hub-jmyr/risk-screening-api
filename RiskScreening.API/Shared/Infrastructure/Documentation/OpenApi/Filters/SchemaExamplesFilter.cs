using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Resources.Requests;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Resources.Responses;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Requests;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Responses;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Filters;

/// <summary>
///     Injects realistic example values into Swagger schemas for request/response DTOs.
///     Keeps examples co-located with the filter instead of polluting the record definitions.
/// </summary>
public class SchemaExamplesFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema) return;

        if (context.Type == typeof(SignInRequest))
            ApplySignInRequest(concreteSchema);
        else if (context.Type == typeof(AuthenticatedUserResponse))
            ApplyAuthenticatedUserResponse(concreteSchema);
        else if (context.Type == typeof(CreateSupplierRequest))
            ApplyCreateSupplierRequest(concreteSchema);
        else if (context.Type == typeof(SupplierResponse))
            ApplySupplierResponse(concreteSchema);
    }

    private static void ApplySignInRequest(OpenApiSchema schema)
    {
        SetExample(schema, "email", "admin@riskscreening.com");
        SetExample(schema, "password", "Admin123!");
    }

    private static void ApplyAuthenticatedUserResponse(OpenApiSchema schema)
    {
        SetExample(schema, "id", "3fa85f64-5717-4562-b3fc-2c963f66afa6");
        SetExample(schema, "email", "admin@riskscreening.com");
        SetExample(schema, "username", "admin");
        SetExample(schema, "status", "Active");
        SetExample(schema, "token", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");

        if (schema.Properties?.TryGetValue("roles", out var roles) == true
            && roles is OpenApiSchema rolesSchema)
            rolesSchema.Example = new JsonArray("Admin");
    }

    private static void ApplyCreateSupplierRequest(OpenApiSchema schema)
    {
        SetExample(schema, "legalName", "Acme Soluciones S.A.C.");
        SetExample(schema, "commercialName", "Acme");
        SetExample(schema, "taxId", "20512345678");
        SetExample(schema, "country", "PE");
        SetExample(schema, "contactPhone", "+51 999 888 777");
        SetExample(schema, "contactEmail", "contacto@acme.pe");
        SetExample(schema, "website", "https://acme.pe");
        SetExample(schema, "address", "Av. Javier Prado 1234, Lima");
        SetExample(schema, "annualBillingUsd", 150000.50);
        SetExample(schema, "notes", "Proveedor referido por el area de compras");
    }

    private static void ApplySupplierResponse(OpenApiSchema schema)
    {
        SetExample(schema, "id", "3fa85f64-5717-4562-b3fc-2c963f66afa6");
        SetExample(schema, "legalName", "Acme Soluciones S.A.C.");
        SetExample(schema, "commercialName", "Acme");
        SetExample(schema, "taxId", "20512345678");
        SetExample(schema, "contactPhone", "+51 999 888 777");
        SetExample(schema, "contactEmail", "contacto@acme.pe");
        SetExample(schema, "website", "https://acme.pe");
        SetExample(schema, "address", "Av. Javier Prado 1234, Lima");
        SetExample(schema, "country", "PE");
        SetExample(schema, "annualBillingUsd", 150000.50);
        SetExample(schema, "riskLevel", "None");
        SetExample(schema, "status", "Pending");
        SetExample(schema, "isDeleted", false);
        SetExample(schema, "notes", "Proveedor referido por el area de compras");
        SetExample(schema, "createdAt", "2026-03-15T00:00:00Z");
        SetExample(schema, "updatedAt", "2026-03-15T00:00:00Z");
        SetExample(schema, "createdBy", "3fa85f64-5717-4562-b3fc-2c963f66afa6");
        SetExample(schema, "updatedBy", "3fa85f64-5717-4562-b3fc-2c963f66afa6");
    }

    private static void SetExample(OpenApiSchema schema, string property, JsonNode value)
    {
        if (schema.Properties?.TryGetValue(property, out var prop) == true
            && prop is OpenApiSchema propSchema)
            propSchema.Example = value;
    }
}

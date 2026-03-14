using Microsoft.OpenApi;
using RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Filters;

/// <summary>
///     Swashbuckle operation filter that reads custom response attributes and automatically
///     adds the corresponding error responses to each Swagger operation.
/// </summary>
/// <remarks>
///     Supported attributes:
///     <list type="bullet">
///         <item><see cref="ApiResponseBadRequestAttribute"/> → 400</item>
///         <item><see cref="ApiResponseUnauthorizedAttribute"/> → 401</item>
///         <item><see cref="ApiResponseForbiddenAttribute"/> → 403</item>
///         <item><see cref="ApiResponseNotFoundAttribute"/> → 404</item>
///         <item><see cref="ApiResponseConflictAttribute"/> → 409</item>
///         <item><see cref="ApiResponsesStandardAttribute"/> → 400 + 401 + 403 + 404</item>
///     </list>
///     All responses use the shared <c>ErrorResponse</c> schema.
/// </remarks>
public class StandardResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodAttrs = context.MethodInfo.GetCustomAttributes(true);
        var classAttrs = context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? [];
        var attrs = methodAttrs.Concat(classAttrs).ToArray();

        var isStandard = attrs.OfType<ApiResponsesStandardAttribute>().Any();

        if (isStandard || attrs.OfType<ApiResponseBadRequestAttribute>().Any())
            AddErrorResponse(operation, "400", "Bad Request — Validation error or malformed request");

        if (isStandard || attrs.OfType<ApiResponseUnauthorizedAttribute>().Any())
            AddErrorResponse(operation, "401", "Unauthorized — Missing or invalid JWT token");

        if (isStandard || attrs.OfType<ApiResponseForbiddenAttribute>().Any())
            AddErrorResponse(operation, "403", "Forbidden — Insufficient permissions");

        if (isStandard || attrs.OfType<ApiResponseNotFoundAttribute>().Any())
            AddErrorResponse(operation, "404", "Not Found — Resource does not exist");

        if (attrs.OfType<ApiResponseConflictAttribute>().Any())
            AddErrorResponse(operation, "409", "Conflict — Resource already exists");
    }

    private static void AddErrorResponse(OpenApiOperation operation, string statusCode, string description)
    {
        if (operation.Responses != null && operation.Responses.ContainsKey(statusCode)) return;

        operation.Responses?.Add(statusCode, new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = new OpenApiSchemaReference("ErrorResponse")
                }
            }
        });
    }
}
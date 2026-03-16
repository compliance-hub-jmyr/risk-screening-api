using System.Text.Json.Serialization;

namespace RiskScreening.API.Shared.Interfaces.REST.Resources;

/// <summary>
///     Standard error response returned by all API endpoints on failure.
///     Follows RFC 7807 Problem Details for HTTP APIs, extended with
///     machine-readable <see cref="ErrorCode"/> / <see cref="ErrorNumber"/>
///     and field-level <see cref="FieldErrors"/> for validation errors.
/// </summary>
/// <example>
/// 404 Not Found:
/// <code>
/// {
///   "type":        "https://tools.ietf.org/html/rfc7807",
///   "title":       "Resource not found",
///   "status":      404,
///   "instance":    "/api/v1/suppliers/3fa85f64",
///   "errorNumber": 4000,
///   "errorCode":   "ENTITY_NOT_FOUND",
///   "message":     "Supplier not found with id: 3fa85f64",
///   "timestamp":   "2026-03-10T10:15:30Z"
/// }
/// </code>
/// 400 Validation Error:
/// <code>
/// {
///   "type":        "https://tools.ietf.org/html/rfc7807",
///   "title":       "Validation failed",
///   "status":      400,
///   "instance":    "/api/v1/suppliers",
///   "errorNumber": 1000,
///   "errorCode":   "VALIDATION_FAILED",
///   "message":     "Request validation failed. Check field errors for details.",
///   "timestamp":   "2026-03-10T10:15:30Z",
///   "fieldErrors": [
///     { "field": "taxId", "message": "TaxId must be exactly 11 digits.", "rejectedValue": "123" }
///   ]
/// }
/// </code>
/// </example>
public record ErrorResponse
{
    // ── RFC 7807 standard fields ──────────────────────────────────────────────

    /// <summary>
    ///     URI reference that identifies the problem type.
    ///     Constant: points to the RFC 7807 specification.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "https://tools.ietf.org/html/rfc7807";

    /// <summary>Short, human-readable summary of the problem type.</summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>HTTP status code. Mirrors the response status for client convenience.</summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>
    ///     URI reference identifying the specific occurrence of the problem.
    ///     Set to the request path (e.g. <c>/api/v1/suppliers</c>).
    /// </summary>
    [JsonPropertyName("instance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instance { get; init; }

    // ── Extension fields (RFC 7807 §3.2 allows additional members) ───────────

    /// <summary>Numeric code for programmatic / support handling (e.g. 4000).</summary>
    [JsonPropertyName("errorNumber")]
    public int ErrorNumber { get; init; }

    /// <summary>
    ///     Machine-readable text code for Angular interceptor switch/case
    ///     (e.g. <c>"ENTITY_NOT_FOUND"</c>).
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>Human-readable error detail. Safe to display to the user.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the error occurred.</summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    ///     Field-level validation errors.
    ///     Only present on 400 validation responses — omitted otherwise.
    /// </summary>
    [JsonPropertyName("fieldErrors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FieldError>? FieldErrors { get; init; }

    /// <summary>Per-field validation error detail.</summary>
    public record FieldError(
        [property: JsonPropertyName("field")] string Field,
        [property: JsonPropertyName("message")]
        string Message,
        [property: JsonPropertyName("rejectedValue")]
        object? RejectedValue
    );
}
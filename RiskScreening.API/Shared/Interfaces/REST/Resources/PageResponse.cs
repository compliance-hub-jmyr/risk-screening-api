using System.Text.Json.Serialization;

namespace RiskScreening.API.Shared.Interfaces.REST.Resources;

/// <summary>
///     Generic wrapper for paginated API responses.
///     Provides a stable JSON structure for pagination across all endpoints.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
/// <example>
/// <code>
/// {
///   "content": [...],
///   "page": {
///     "number": 0,
///     "size": 20,
///     "totalElements": 150,
///     "totalPages": 8,
///     "first": true,
///     "last": false,
///     "hasNext": true,
///     "hasPrevious": false
///   }
/// }
/// </code>
/// </example>
public record PageResponse<T>(
    [property: JsonPropertyName("content")]
    List<T> Content,
    [property: JsonPropertyName("meta")] PageResponse<T>.PageMetadata Page
)
{
    /// <summary>Pagination metadata.</summary>
    public record PageMetadata(
        [property: JsonPropertyName("number")] int Number,
        [property: JsonPropertyName("size")] int Size,
        [property: JsonPropertyName("totalElements")]
        long TotalElements,
        [property: JsonPropertyName("totalPages")]
        int TotalPages,
        [property: JsonPropertyName("first")] bool First,
        [property: JsonPropertyName("last")] bool Last,
        [property: JsonPropertyName("hasNext")]
        bool HasNext,
        [property: JsonPropertyName("hasPrevious")]
        bool HasPrevious
    );
}
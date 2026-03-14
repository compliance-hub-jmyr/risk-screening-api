namespace RiskScreening.API.Shared.Infrastructure.Persistence.Query.Pagination;

/// <summary>
///     Validated pagination and sorting parameters received from a request.
/// </summary>
/// <remarks>
///     Defaults: page = 0, size = 20. Maximum size: 100.
///     Negative page → 0. Size exceeding max → capped at 100.
/// </remarks>
/// <example>
/// <code>
/// [HttpGet]
/// public async Task&lt;IActionResult&gt; GetAll(
///     [FromQuery] int? page,
///     [FromQuery] int? size,
///     [FromQuery] string? sortBy,
///     [FromQuery] string? sortDirection,
///     CancellationToken ct)
/// {
///     var request = new PageRequest(page, size, sortBy, sortDirection);
///     ...
/// }
/// </code>
/// </example>
public record PageRequest
{
    public const int DefaultPage = 0;
    public const int DefaultSize = 20;
    public const int MaxSize = 100;

    /// <summary>Zero-based page number. Always &gt;= 0.</summary>
    public int Page { get; init; }

    /// <summary>Number of items per page. Between 1 and <see cref="MaxSize"/>.</summary>
    public int Size { get; init; }

    /// <summary>Field name to sort by. Null uses the entity's default sort.</summary>
    public string? SortBy { get; init; }

    /// <summary>Sort direction: "ASC" or "DESC". Null uses the entity's default direction.</summary>
    public string? SortDirection { get; init; }

    public PageRequest(
        int? page = null,
        int? size = null,
        string? sortBy = null,
        string? sortDirection = null)
    {
        Page = page is >= 0 ? page.Value : DefaultPage;
        Size = size is > 0 ? Math.Min(size.Value, MaxSize) : DefaultSize;
        SortBy = sortBy;
        SortDirection = sortDirection;
    }
}
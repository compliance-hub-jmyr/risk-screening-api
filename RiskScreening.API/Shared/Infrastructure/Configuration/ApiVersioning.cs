namespace RiskScreening.API.Shared.Infrastructure.Configuration;

/// <summary>
/// API Versioning Constants.
/// <para>
/// Uses header-based API versioning with the <c>Api-Version</c> header.
/// </para>
/// <para>
/// <strong>Strategy:</strong>
/// <list type="bullet">
///   <item>Base path: <c>/api</c></item>
///   <item>Version specified via <c>Api-Version</c> request header</item>
///   <item>Default version: <c>1.0</c> — clients without the header receive V1</item>
/// </list>
/// </para>
/// <para>
/// <strong>Client usage:</strong>
/// <code>
/// GET /api/users
/// Api-Version: 1
/// </code>
/// </para>
/// </summary>
public static class ApiVersioning
{
    /// <summary>API base path for all REST endpoints.</summary>
    public const string Base = "/api";

    /// <summary>Version 1 string.</summary>
    public const string V1 = "1.0";

    /// <summary>Version 2 string (reserved for future use).</summary>
    public const string V2 = "2.0";

    /// <summary>Header name used to specify the requested API version.</summary>
    public const string Header = "Api-Version";
}
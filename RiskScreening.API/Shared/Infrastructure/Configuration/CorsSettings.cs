namespace RiskScreening.API.Shared.Infrastructure.Configuration;

/// <summary>
///     Strongly typed settings for the CORS policy, bound from the <c>Cors</c>
///     section in <c>appsettings.json</c>.
/// </summary>
public class CorsSettings
{
    /// <summary>
    ///     The list of allowed origins for the CORS policy.
    ///     Example: <c>["http://localhost:4200", "https://app.riskscreening.com"]</c>
    ///     If empty, the policy falls back to allowing any origin (development only).
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}

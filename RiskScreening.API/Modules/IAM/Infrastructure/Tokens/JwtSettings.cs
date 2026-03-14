namespace RiskScreening.API.Modules.IAM.Infrastructure.Tokens;

public class JwtSettings
{
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpirationHours { get; init; } = 24;
}
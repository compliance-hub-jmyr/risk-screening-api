using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Shared.Domain.Model.ValueObjects;

/// <summary>Valid HTTP or HTTPS URL.</summary>
public record WebsiteUrl : ValueObject
{
    public string Value { get; }

    public WebsiteUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException("WebsiteUrl", value, "must not be blank",
                ErrorCodes.InvalidWebsiteUrl, ErrorCodes.InvalidWebsiteUrlCode);

        var trimmed = value.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidValueException("WebsiteUrl", value,
                "must be a valid HTTP or HTTPS URL",
                ErrorCodes.InvalidWebsiteUrl, ErrorCodes.InvalidWebsiteUrlCode);

        Value = trimmed;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(WebsiteUrl url)
    {
        return url.Value;
    }
}
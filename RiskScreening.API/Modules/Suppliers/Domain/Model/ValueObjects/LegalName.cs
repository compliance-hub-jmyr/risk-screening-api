using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;

/// <summary>Supplier legal name (razón social) — alphanumeric, max 200 characters.</summary>
public record LegalName : ValueObject, IComparable<LegalName>
{
    public const int MaxLength = 200;

    public string Value { get; }

    public LegalName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException("LegalName", value, "must not be blank",
                ErrorCodes.InvalidLegalName, ErrorCodes.InvalidLegalNameCode);

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new InvalidValueException("LegalName", value,
                $"must not exceed {MaxLength} characters",
                ErrorCodes.InvalidLegalName, ErrorCodes.InvalidLegalNameCode);

        Value = trimmed;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(LegalName name)
    {
        return name.Value;
    }

    public int CompareTo(LegalName? other)
    {
        return string.Compare(Value, other?.Value, StringComparison.Ordinal);
    }
}
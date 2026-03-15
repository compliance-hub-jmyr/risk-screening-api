using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;

/// <summary>Supplier commercial name (nombre comercial) — alphanumeric, max 200 characters.</summary>
public record CommercialName : ValueObject, IComparable<CommercialName>
{
    public const int MaxLength = 200;

    public string Value { get; }

    public CommercialName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException("CommercialName", value, "must not be blank",
                ErrorCodes.InvalidCommercialName, ErrorCodes.InvalidCommercialNameCode);

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new InvalidValueException("CommercialName", value,
                $"must not exceed {MaxLength} characters",
                ErrorCodes.InvalidCommercialName, ErrorCodes.InvalidCommercialNameCode);

        Value = trimmed;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(CommercialName name)
    {
        return name.Value;
    }

    public int CompareTo(CommercialName? other)
    {
        return string.Compare(Value, other?.Value, StringComparison.Ordinal);
    }
}
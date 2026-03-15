using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Shared.Domain.Model.ValueObjects;

/// <summary>Strongly-typed identifier for a Supplier aggregate (UUID v4 format).</summary>
public record SupplierId : ValueObject
{
    public string Value { get; }

    public SupplierId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException("SupplierId", value, "must not be blank",
                ErrorCodes.InvalidSupplierId, ErrorCodes.InvalidSupplierIdCode);

        if (!Guid.TryParse(value, out _))
            throw new InvalidValueException("SupplierId", value, "must be a valid UUID",
                ErrorCodes.InvalidSupplierId, ErrorCodes.InvalidSupplierIdCode);

        Value = value;
    }

    public override string ToString() => Value;
    public static implicit operator string(SupplierId id) => id.Value;
}

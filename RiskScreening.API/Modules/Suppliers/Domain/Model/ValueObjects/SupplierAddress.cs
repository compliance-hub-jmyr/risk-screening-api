using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;

/// <summary>Supplier physical address — alphanumeric, max 500 characters.</summary>
public record SupplierAddress : ValueObject
{
    public const int MaxLength = 500;

    public string Value { get; }

    public SupplierAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException("SupplierAddress", value, "must not be blank",
                ErrorCodes.InvalidSupplierAddress, ErrorCodes.InvalidSupplierAddressCode);

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new InvalidValueException("SupplierAddress", value,
                $"must not exceed {MaxLength} characters",
                ErrorCodes.InvalidSupplierAddress, ErrorCodes.InvalidSupplierAddressCode);

        Value = trimmed;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(SupplierAddress address)
    {
        return address.Value;
    }
}
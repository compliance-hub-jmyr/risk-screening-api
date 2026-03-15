using System.Globalization;
using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;

/// <summary>Annual billing in USD — non-negative, max 2 decimal places (accounting precision).</summary>
public record AnnualBilling : ValueObject
{
    public decimal Value { get; }

    public AnnualBilling(decimal value)
    {
        if (value < 0)
            throw new InvalidValueException("AnnualBilling", value.ToString("F2"),
                "must be a non-negative value",
                ErrorCodes.InvalidAnnualBilling, ErrorCodes.InvalidAnnualBillingCode);

        if (decimal.Round(value, 2) != value)
            throw new InvalidValueException("AnnualBilling", value.ToString(CultureInfo.InvariantCulture),
                "must have at most 2 decimal places",
                ErrorCodes.InvalidAnnualBilling, ErrorCodes.InvalidAnnualBillingCode);

        Value = value;
    }

    public override string ToString()
    {
        return Value.ToString("F2");
    }

    public static implicit operator decimal(AnnualBilling billing)
    {
        return billing.Value;
    }
}
using System.Text.RegularExpressions;
using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;

/// <summary>Supplier tax identification number — exactly 11 digits (e.g., Peru RUC).</summary>
public partial record TaxId : ValueObject
{
    public string Value { get; }

    public TaxId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException("TaxId", value, "must not be blank",
                ErrorCodes.InvalidTaxId, ErrorCodes.InvalidTaxIdCode);

        var trimmed = value.Trim();

        if (!ElevenDigits().IsMatch(trimmed))
            throw new InvalidValueException("TaxId", value,
                "must be exactly 11 digits",
                ErrorCodes.InvalidTaxId, ErrorCodes.InvalidTaxIdCode);

        Value = trimmed;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(TaxId taxId)
    {
        return taxId.Value;
    }

    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex ElevenDigits();
}
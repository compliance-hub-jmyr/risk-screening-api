using Bogus;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.UnitTests.Modules.Suppliers.Mothers;

/// <summary>
///     Object Mother for <see cref="CreateSupplierCommand"/> — named business scenarios with random valid data.
/// </summary>
public static class CreateSupplierCommandMother
{
    private static readonly Faker Faker = new();
    private static readonly string[] ValidCountryCodes = CountryCode.ValidCodes.ToArray();

    /// <summary>A fully valid command with all fields populated randomly.</summary>
    public static CreateSupplierCommand Valid(string? taxId = null)
    {
        return new CreateSupplierCommand(
            LegalName: Faker.Company.CompanyName(),
            CommercialName: Faker.Company.CompanyName(),
            TaxId: taxId ?? Faker.Random.String2(11, "0123456789"),
            Country: Faker.PickRandom(ValidCountryCodes),
            ContactPhone: $"+{Faker.Random.Int(1, 99)} {Faker.Random.String2(9, "0123456789")}",
            ContactEmail: Faker.Internet.Email(),
            Website: $"https://{Faker.Internet.DomainName()}",
            Address: Faker.Address.FullAddress(),
            AnnualBillingUsd: Math.Round(Faker.Random.Decimal(0, 10_000_000), 2),
            Notes: Faker.Lorem.Sentence());
    }

    /// <summary>A valid command with only required fields — optional fields are null.</summary>
    public static CreateSupplierCommand Minimal()
    {
        return new CreateSupplierCommand(
            LegalName: Faker.Company.CompanyName(),
            CommercialName: Faker.Company.CompanyName(),
            TaxId: Faker.Random.String2(11, "0123456789"),
            Country: Faker.PickRandom(ValidCountryCodes),
            ContactPhone: null,
            ContactEmail: null,
            Website: null,
            Address: null,
            AnnualBillingUsd: null,
            Notes: null);
    }

    /// <summary>A command whose TaxId already exists in the system.</summary>
    public static CreateSupplierCommand WithDuplicateTaxId(string existingTaxId)
    {
        return Valid(existingTaxId);
    }

    // ── Invalid required fields ─────────────────────────────────────────────

    public static CreateSupplierCommand WithBlankLegalName()
    {
        return Valid() with { LegalName = "   " };
    }

    public static CreateSupplierCommand WithLegalNameTooLong()
    {
        return Valid() with { LegalName = Faker.Random.String2(201) };
    }

    public static CreateSupplierCommand WithBlankCommercialName()
    {
        return Valid() with { CommercialName = "" };
    }

    public static CreateSupplierCommand WithCommercialNameTooLong()
    {
        return Valid() with { CommercialName = Faker.Random.String2(201) };
    }

    public static CreateSupplierCommand WithInvalidTaxId()
    {
        return Valid() with { TaxId = Faker.Random.String2(Faker.Random.Int(1, 10), "0123456789") };
    }

    public static CreateSupplierCommand WithNonNumericTaxId()
    {
        return Valid() with { TaxId = Faker.Random.String2(10, "0123456789") + Faker.Random.Char('A', 'Z') };
    }

    public static CreateSupplierCommand WithInvalidCountryCode()
    {
        return Valid() with { Country = "XX" };
    }

    // ── Invalid optional fields ─────────────────────────────────────────────

    public static CreateSupplierCommand WithInvalidEmail()
    {
        return Valid() with { ContactEmail = Faker.Random.AlphaNumeric(10) };
    }

    public static CreateSupplierCommand WithInvalidPhone()
    {
        return Valid() with { ContactPhone = Faker.Random.String2(5, "abcdefg") };
    }

    public static CreateSupplierCommand WithInvalidWebsite()
    {
        return Valid() with { Website = Faker.Random.AlphaNumeric(15) };
    }

    public static CreateSupplierCommand WithNegativeBilling()
    {
        return Valid() with { AnnualBillingUsd = -Faker.Random.Decimal(0.01m, 1_000_000m) };
    }

    public static CreateSupplierCommand WithBillingTooManyDecimals()
    {
        return Valid() with { AnnualBillingUsd = Faker.Random.Decimal(100, 10_000) + 0.009m };
    }

    public static CreateSupplierCommand WithAddressTooLong()
    {
        return Valid() with { Address = Faker.Random.String2(501) };
    }
}

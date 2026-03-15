using Bogus;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.UnitTests.Modules.Suppliers.Mothers;

/// <summary>
///     Object Mother for <see cref="UpdateSupplierCommand"/> — named business scenarios with random valid data.
/// </summary>
public static class UpdateSupplierCommandMother
{
    private static readonly Faker Faker = new();
    private static readonly string[] ValidCountryCodes = CountryCode.ValidCodes.ToArray();

    /// <summary>A fully valid command with all fields populated randomly.</summary>
    public static UpdateSupplierCommand Valid(string id, string? taxId = null)
    {
        return new UpdateSupplierCommand(
            Id: id,
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
    public static UpdateSupplierCommand Minimal(string id, string? taxId = null)
    {
        return new UpdateSupplierCommand(
            Id: id,
            LegalName: Faker.Company.CompanyName(),
            CommercialName: Faker.Company.CompanyName(),
            TaxId: taxId ?? Faker.Random.String2(11, "0123456789"),
            Country: Faker.PickRandom(ValidCountryCodes),
            ContactPhone: null,
            ContactEmail: null,
            Website: null,
            Address: null,
            AnnualBillingUsd: null,
            Notes: null);
    }

    // ── Invalid required fields ─────────────────────────────────────────────

    public static UpdateSupplierCommand WithBlankLegalName(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { LegalName = "   " };
    }

    public static UpdateSupplierCommand WithLegalNameTooLong(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { LegalName = Faker.Random.String2(201) };
    }

    public static UpdateSupplierCommand WithBlankCommercialName(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { CommercialName = "" };
    }

    public static UpdateSupplierCommand WithCommercialNameTooLong(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { CommercialName = Faker.Random.String2(201) };
    }

    public static UpdateSupplierCommand WithInvalidTaxId(string id)
    {
        return Valid(id) with { TaxId = Faker.Random.String2(Faker.Random.Int(1, 10), "0123456789") };
    }

    public static UpdateSupplierCommand WithNonNumericTaxId(string id)
    {
        return Valid(id) with { TaxId = Faker.Random.String2(10, "0123456789") + Faker.Random.Char('A', 'Z') };
    }

    public static UpdateSupplierCommand WithInvalidCountryCode(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { Country = "XX" };
    }

    // ── Invalid optional fields ─────────────────────────────────────────────

    public static UpdateSupplierCommand WithInvalidEmail(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { ContactEmail = Faker.Random.AlphaNumeric(10) };
    }

    public static UpdateSupplierCommand WithInvalidPhone(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { ContactPhone = Faker.Random.String2(5, "abcdefg") };
    }

    public static UpdateSupplierCommand WithInvalidWebsite(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { Website = Faker.Random.AlphaNumeric(15) };
    }

    public static UpdateSupplierCommand WithNegativeBilling(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { AnnualBillingUsd = -Faker.Random.Decimal(0.01m, 1_000_000m) };
    }

    public static UpdateSupplierCommand WithBillingTooManyDecimals(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { AnnualBillingUsd = Faker.Random.Decimal(100, 10_000) + 0.009m };
    }

    public static UpdateSupplierCommand WithAddressTooLong(string id, string? taxId = null)
    {
        return Valid(id, taxId) with { Address = Faker.Random.String2(501) };
    }
}

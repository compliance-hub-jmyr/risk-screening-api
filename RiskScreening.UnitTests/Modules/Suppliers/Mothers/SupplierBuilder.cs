using Bogus;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.UnitTests.Modules.Suppliers.Mothers;

/// <summary>
///     Fluent builder for creating <see cref="Supplier"/> instances in tests.
///     Uses Bogus to generate valid data — only specify fields relevant to the test.
/// </summary>
public class SupplierBuilder
{
    private static readonly Faker Faker = new();
    private static readonly string[] CountryCodes = CountryCode.ValidCodes.ToArray();

    private string _legalName = Faker.Company.CompanyName();
    private string _commercialName = Faker.Company.CompanyName();
    private string _taxId = Faker.Random.String2(11, "0123456789");
    private string _country = Faker.PickRandom(CountryCodes);
    private string? _contactPhone = Faker.Phone.PhoneNumber("+## ### ### ###");
    private string? _contactEmail = Faker.Internet.Email();
    private string? _website = $"https://{Faker.Internet.DomainName()}";
    private string? _address = Faker.Address.FullAddress();
    private decimal? _annualBillingUsd = Math.Round(Faker.Random.Decimal(0, 10_000_000), 2);
    private string? _notes = Faker.Lorem.Sentence();
    private bool _deleted;

    public static SupplierBuilder ASupplier()
    {
        return new SupplierBuilder();
    }

    public SupplierBuilder WithTaxId(string taxId)
    {
        _taxId = taxId;
        return this;
    }

    public SupplierBuilder WithLegalName(string name)
    {
        _legalName = name;
        return this;
    }

    public SupplierBuilder WithCommercialName(string name)
    {
        _commercialName = name;
        return this;
    }

    public SupplierBuilder WithCountry(string country)
    {
        _country = country;
        return this;
    }

    public SupplierBuilder WithContactPhone(string? phone)
    {
        _contactPhone = phone;
        return this;
    }

    public SupplierBuilder WithContactEmail(string? email)
    {
        _contactEmail = email;
        return this;
    }

    public SupplierBuilder WithWebsite(string? website)
    {
        _website = website;
        return this;
    }

    public SupplierBuilder WithAddress(string? address)
    {
        _address = address;
        return this;
    }

    public SupplierBuilder WithAnnualBillingUsd(decimal? billing)
    {
        _annualBillingUsd = billing;
        return this;
    }

    public SupplierBuilder WithNotes(string? notes)
    {
        _notes = notes;
        return this;
    }

    public SupplierBuilder WithNoOptionalFields()
    {
        _contactPhone = null;
        _contactEmail = null;
        _website = null;
        _address = null;
        _annualBillingUsd = null;
        return this;
    }

    public SupplierBuilder Deleted()
    {
        _deleted = true;
        return this;
    }

    public Supplier Build()
    {
        var supplier = Supplier.Create(
            _legalName,
            _commercialName,
            _taxId,
            _country,
            _contactPhone,
            _contactEmail,
            _website,
            _address,
            _annualBillingUsd,
            _notes);

        if (_deleted)
            supplier.Delete();

        return supplier;
    }
}
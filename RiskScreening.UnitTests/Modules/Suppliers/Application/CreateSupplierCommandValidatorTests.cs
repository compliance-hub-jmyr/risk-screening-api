using AwesomeAssertions;
using RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Create;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.UnitTests.Modules.Suppliers.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.Suppliers.Application;

/// <summary>
///     Unit tests for <see cref="CreateSupplierCommandValidator"/>.
///     The validator is a thin layer — it only checks that required fields are not empty.
///     All format/business validation belongs in the domain Value Objects.
/// </summary>
public class CreateSupplierCommandValidatorTests
{
    private readonly CreateSupplierCommandValidator _validator = new();

    // Success paths

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = CreateSupplierCommandMother.Valid();
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MinimalRequiredFields_PassesValidation()
    {
        var command = CreateSupplierCommandMother.Minimal();
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    // Failure paths 

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyLegalName_FailsValidation(string? legalName)
    {
        var command = CreateSupplierCommandMother.Valid() with { LegalName = legalName! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierCommand.LegalName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyCommercialName_FailsValidation(string? commercialName)
    {
        var command = CreateSupplierCommandMother.Valid() with { CommercialName = commercialName! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierCommand.CommercialName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyTaxId_FailsValidation(string? taxId)
    {
        var command = CreateSupplierCommandMother.Valid() with { TaxId = taxId! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierCommand.TaxId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyCountry_FailsValidation(string? country)
    {
        var command = CreateSupplierCommandMother.Valid() with { Country = country! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierCommand.Country));
    }
}

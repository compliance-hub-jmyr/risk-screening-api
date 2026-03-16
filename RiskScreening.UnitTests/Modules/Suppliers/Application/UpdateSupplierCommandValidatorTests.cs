using AwesomeAssertions;
using RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Update;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.UnitTests.Modules.Suppliers.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.Suppliers.Application;

/// <summary>
///     Unit tests for <see cref="UpdateSupplierCommandValidator"/>.
///     The validator is a thin layer — it only checks that required fields are not empty.
///     All format/business validation belongs in the domain Value Objects.
/// </summary>
public class UpdateSupplierCommandValidatorTests
{
    private readonly UpdateSupplierCommandValidator _validator = new();
    private static readonly string ValidId = Guid.NewGuid().ToString();

    // Success paths

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = UpdateSupplierCommandMother.Valid(ValidId);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MinimalRequiredFields_PassesValidation()
    {
        var command = UpdateSupplierCommandMother.Minimal(ValidId);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    // Failure paths

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyId_FailsValidation(string? id)
    {
        var command = UpdateSupplierCommandMother.Valid(ValidId) with { Id = id! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSupplierCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyLegalName_FailsValidation(string? legalName)
    {
        var command = UpdateSupplierCommandMother.Valid(ValidId) with { LegalName = legalName! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSupplierCommand.LegalName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyCommercialName_FailsValidation(string? commercialName)
    {
        var command = UpdateSupplierCommandMother.Valid(ValidId) with { CommercialName = commercialName! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSupplierCommand.CommercialName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyTaxId_FailsValidation(string? taxId)
    {
        var command = UpdateSupplierCommandMother.Valid(ValidId) with { TaxId = taxId! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSupplierCommand.TaxId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyCountry_FailsValidation(string? country)
    {
        var command = UpdateSupplierCommandMother.Valid(ValidId) with { Country = country! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSupplierCommand.Country));
    }
}

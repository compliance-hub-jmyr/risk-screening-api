using AwesomeAssertions;
using Bogus;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Create;
using RiskScreening.API.Modules.Suppliers.Domain.Exceptions;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Domain.Repositories;
using RiskScreening.UnitTests.Modules.Suppliers.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.Suppliers.Application;

/// <summary>
///     Unit tests for <see cref="CreateSupplierCommandHandler"/>.
///     All external dependencies are substituted — no I/O involved.
/// </summary>
public class CreateSupplierCommandHandlerTests
{
    private static readonly Faker Faker = new();

    private readonly ISupplierRepository _supplierRepository = Substitute.For<ISupplierRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ILogger<CreateSupplierCommandHandler> _logger =
        Substitute.For<ILogger<CreateSupplierCommandHandler>>();

    private readonly CreateSupplierCommandHandler _sut;

    public CreateSupplierCommandHandlerTests()
    {
        _sut = new CreateSupplierCommandHandler(_supplierRepository, _unitOfWork, _logger);
    }

    // Success paths

    [Fact]
    public async Task Handle_ValidCommand_CreatesSupplierWithPendingStatusAndNoneRisk()
    {
        // Arrange
        var command = CreateSupplierCommandMother.Valid();
        _supplierRepository.ExistsByTaxIdAsync(command.TaxId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(SupplierStatus.Pending);
        result.RiskLevel.Should().Be(RiskLevel.None);
        result.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsAndCompletesUnitOfWork()
    {
        // Arrange
        var command = CreateSupplierCommandMother.Valid();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _supplierRepository.Received(1).AddAsync(Arg.Any<Supplier>());
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_MapsAllRequiredFieldsCorrectly()
    {
        // Arrange
        var command = CreateSupplierCommandMother.Valid();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.LegalName.Value.Should().Be(command.LegalName);
        result.CommercialName.Value.Should().Be(command.CommercialName);
        result.TaxId.Value.Should().Be(command.TaxId);
        result.Country.Value.Should().Be(command.Country);
    }

    [Fact]
    public async Task Handle_ValidCommand_MapsAllOptionalFieldsCorrectly()
    {
        // Arrange
        var command = CreateSupplierCommandMother.Valid();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ContactPhone!.Value.Should().Be(command.ContactPhone);
        result.ContactEmail!.Value.Should().Be(command.ContactEmail!.Trim().ToLowerInvariant());
        result.Website!.Value.Should().Be(command.Website);
        result.Address!.Value.Should().Be(command.Address);
        result.AnnualBillingUsd!.Value.Should().Be(command.AnnualBillingUsd!.Value);
    }

    [Fact]
    public async Task Handle_MinimalCommand_CreatesSupplierWithNullOptionalFields()
    {
        // Arrange
        var command = CreateSupplierCommandMother.Minimal();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ContactPhone.Should().BeNull();
        result.ContactEmail.Should().BeNull();
        result.Website.Should().BeNull();
        result.Address.Should().BeNull();
        result.AnnualBillingUsd.Should().BeNull();
        result.Notes.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidCommand_GeneratesNonEmptyId()
    {
        // Arrange
        var command = CreateSupplierCommandMother.Valid();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    // Failure paths 
    
    [Fact]
    public async Task Handle_DuplicateTaxId_ThrowsSupplierTaxIdAlreadyExistsException()
    {
        // Arrange
        var taxId = Faker.Random.String2(11, "0123456789");
        var command = CreateSupplierCommandMother.WithDuplicateTaxId(taxId);
        _supplierRepository.ExistsByTaxIdAsync(taxId, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<SupplierTaxIdAlreadyExistsException>(
            () => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateTaxId_DoesNotPersist()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithDuplicateTaxId("12345678901");
        _supplierRepository.ExistsByTaxIdAsync("12345678901", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await Assert.ThrowsAsync<SupplierTaxIdAlreadyExistsException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert — nothing was persisted
        await _supplierRepository.DidNotReceive().AddAsync(Arg.Any<Supplier>());
        await _unitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankLegalName_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithBlankLegalName();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("LegalName");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidLegalNameCode);
    }

    [Fact]
    public async Task Handle_LegalNameTooLong_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithLegalNameTooLong();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("LegalName");
    }

    [Fact]
    public async Task Handle_BlankCommercialName_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithBlankCommercialName();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("CommercialName");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidCommercialNameCode);
    }

    [Fact]
    public async Task Handle_CommercialNameTooLong_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithCommercialNameTooLong();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("CommercialName");
    }

    [Fact]
    public async Task Handle_InvalidTaxIdFormat_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithInvalidTaxId();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("TaxId");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidTaxIdCode);
    }

    [Fact]
    public async Task Handle_NonNumericTaxId_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithNonNumericTaxId();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("TaxId");
    }

    [Fact]
    public async Task Handle_InvalidCountryCode_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithInvalidCountryCode();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("CountryCode");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidCountryCodeCode);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithInvalidEmail();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("Email");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidEmailCode);
    }

    [Fact]
    public async Task Handle_InvalidPhone_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithInvalidPhone();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("PhoneNumber");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidPhoneNumberCode);
    }

    [Fact]
    public async Task Handle_InvalidWebsite_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithInvalidWebsite();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("WebsiteUrl");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidWebsiteUrlCode);
    }

    [Fact]
    public async Task Handle_NegativeBilling_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithNegativeBilling();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("AnnualBilling");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidAnnualBillingCode);
    }

    [Fact]
    public async Task Handle_BillingTooManyDecimals_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithBillingTooManyDecimals();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("AnnualBilling");
    }

    [Fact]
    public async Task Handle_AddressTooLong_ThrowsInvalidValueException()
    {
        // Arrange
        var command = CreateSupplierCommandMother.WithAddressTooLong();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("SupplierAddress");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidSupplierAddressCode);
    }

    [Fact]
    public async Task Handle_InvalidDomainData_DoesNotPersist()
    {
        // Arrange — any VO validation failure
        var command = CreateSupplierCommandMother.WithInvalidTaxId();
        _supplierRepository.ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await Assert.ThrowsAsync<InvalidValueException>(
            () => _sut.Handle(command, CancellationToken.None));

        // Assert — nothing was persisted
        await _supplierRepository.DidNotReceive().AddAsync(Arg.Any<Supplier>());
        await _unitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }
}

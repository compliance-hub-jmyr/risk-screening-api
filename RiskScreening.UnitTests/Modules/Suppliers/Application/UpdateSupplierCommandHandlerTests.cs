using AwesomeAssertions;
using Bogus;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Update;
using RiskScreening.API.Modules.Suppliers.Domain.Exceptions;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Domain.Repositories;
using RiskScreening.UnitTests.Modules.Suppliers.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.Suppliers.Application;

/// <summary>
///     Unit tests for <see cref="UpdateSupplierCommandHandler"/>.
///     All external dependencies are substituted — no I/O involved.
/// </summary>
public class UpdateSupplierCommandHandlerTests
{
    private static readonly Faker Faker = new();

    private readonly ISupplierRepository _supplierRepository = Substitute.For<ISupplierRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ILogger<UpdateSupplierCommandHandler> _logger =
        Substitute.For<ILogger<UpdateSupplierCommandHandler>>();

    private readonly UpdateSupplierCommandHandler _sut;

    public UpdateSupplierCommandHandlerTests()
    {
        _sut = new UpdateSupplierCommandHandler(_supplierRepository, _unitOfWork, _logger);
    }

    // Success paths

    [Fact]
    public async Task Handle_ExistingSupplier_UpdatesAllRequiredFieldsCorrectly()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.Valid(supplier.Id, supplier.TaxId.Value);

        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.LegalName.Value.Should().Be(command.LegalName);
        result.CommercialName.Value.Should().Be(command.CommercialName);
        result.TaxId.Value.Should().Be(command.TaxId);
        result.Country.Value.Should().Be(command.Country);
    }

    [Fact]
    public async Task Handle_ExistingSupplier_UpdatesAllOptionalFieldsCorrectly()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.Valid(supplier.Id, supplier.TaxId.Value);

        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

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
    public async Task Handle_ExistingSupplier_PersistsAndCompletesUnitOfWork()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.Valid(supplier.Id, supplier.TaxId.Value);

        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _supplierRepository.Received(1).Update(supplier);
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameTaxId_SkipsDuplicateCheckAndUpdates()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.Valid(supplier.Id, supplier.TaxId.Value);

        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — ExistsByTaxIdAsync should NOT have been called since TaxId didn't change
        result.Should().NotBeNull();
        await _supplierRepository.DidNotReceive().ExistsByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _supplierRepository.Received(1).Update(supplier);
    }

    [Fact]
    public async Task Handle_MinimalCommand_SetsOptionalFieldsToNull()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.Minimal(supplier.Id, supplier.TaxId.Value);

        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

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
    public async Task Handle_ChangedTaxIdNotDuplicate_UpdatesSuccessfully()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var newTaxId = Faker.Random.String2(11, "0123456789");
        var command = UpdateSupplierCommandMother.Valid(supplier.Id, newTaxId);

        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);
        _supplierRepository.ExistsByTaxIdAsync(newTaxId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.TaxId.Value.Should().Be(newTaxId);
        _supplierRepository.Received(1).Update(supplier);
    }

    // Failure paths

    [Fact]
    public async Task Handle_NonExistentSupplier_ThrowsSupplierNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _supplierRepository.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Supplier?)null);

        // Act & Assert
        await Assert.ThrowsAsync<SupplierNotFoundException>(() =>
            _sut.Handle(UpdateSupplierCommandMother.Valid(id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeletedSupplier_ThrowsSupplierNotFoundException()
    {
        // Arrange
        var supplier = SupplierMother.Deleted().Build();
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act & Assert
        await Assert.ThrowsAsync<SupplierNotFoundException>(() =>
            _sut.Handle(UpdateSupplierCommandMother.Valid(supplier.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonExistentSupplier_DoesNotPersist()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _supplierRepository.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Supplier?)null);

        // Act
        await Assert.ThrowsAsync<SupplierNotFoundException>(() =>
            _sut.Handle(UpdateSupplierCommandMother.Valid(id), CancellationToken.None));

        // Assert
        _supplierRepository.DidNotReceive().Update(Arg.Any<Supplier>());
        await _unitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ChangedTaxIdAlreadyExists_ThrowsSupplierTaxIdAlreadyExistsException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var newTaxId = Faker.Random.String2(11, "0123456789");
        var command = UpdateSupplierCommandMother.Valid(supplier.Id, newTaxId);

        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);
        _supplierRepository.ExistsByTaxIdAsync(newTaxId, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<SupplierTaxIdAlreadyExistsException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateTaxId_DoesNotPersist()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var newTaxId = Faker.Random.String2(11, "0123456789");
        var command = UpdateSupplierCommandMother.Valid(supplier.Id, newTaxId);

        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);
        _supplierRepository.ExistsByTaxIdAsync(newTaxId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await Assert.ThrowsAsync<SupplierTaxIdAlreadyExistsException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        _supplierRepository.DidNotReceive().Update(Arg.Any<Supplier>());
        await _unitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankLegalName_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithBlankLegalName(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("LegalName");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidLegalNameCode);
    }

    [Fact]
    public async Task Handle_LegalNameTooLong_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithLegalNameTooLong(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("LegalName");
    }

    [Fact]
    public async Task Handle_BlankCommercialName_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithBlankCommercialName(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("CommercialName");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidCommercialNameCode);
    }

    [Fact]
    public async Task Handle_CommercialNameTooLong_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithCommercialNameTooLong(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("CommercialName");
    }

    [Fact]
    public async Task Handle_InvalidTaxIdFormat_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithInvalidTaxId(supplier.Id);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);
        _supplierRepository.ExistsByTaxIdAsync(command.TaxId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("TaxId");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidTaxIdCode);
    }

    [Fact]
    public async Task Handle_NonNumericTaxId_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithNonNumericTaxId(supplier.Id);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);
        _supplierRepository.ExistsByTaxIdAsync(command.TaxId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("TaxId");
    }

    [Fact]
    public async Task Handle_InvalidCountryCode_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithInvalidCountryCode(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("CountryCode");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidCountryCodeCode);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithInvalidEmail(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("Email");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidEmailCode);
    }

    [Fact]
    public async Task Handle_InvalidPhone_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithInvalidPhone(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("PhoneNumber");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidPhoneNumberCode);
    }

    [Fact]
    public async Task Handle_InvalidWebsite_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithInvalidWebsite(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("WebsiteUrl");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidWebsiteUrlCode);
    }

    [Fact]
    public async Task Handle_NegativeBilling_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithNegativeBilling(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("AnnualBilling");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidAnnualBillingCode);
    }

    [Fact]
    public async Task Handle_BillingTooManyDecimals_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithBillingTooManyDecimals(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("AnnualBilling");
    }

    [Fact]
    public async Task Handle_AddressTooLong_ThrowsInvalidValueException()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithAddressTooLong(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert
        ex.ValueObjectName.Should().Be("SupplierAddress");
        ex.ErrorCode.Should().Be(ErrorCodes.InvalidSupplierAddressCode);
    }

    [Fact]
    public async Task Handle_InvalidDomainData_DoesNotPersist()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        var command = UpdateSupplierCommandMother.WithInvalidCountryCode(supplier.Id, supplier.TaxId.Value);
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        await Assert.ThrowsAsync<InvalidValueException>(() =>
            _sut.Handle(command, CancellationToken.None));

        // Assert — nothing was persisted
        _supplierRepository.DidNotReceive().Update(Arg.Any<Supplier>());
        await _unitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }
}

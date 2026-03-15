using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Queries;
using RiskScreening.API.Modules.Suppliers.Domain.Exceptions;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Queries;
using RiskScreening.UnitTests.Modules.Suppliers.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.Suppliers.Application;

/// <summary>
///     Unit tests for <see cref="GetSupplierByIdQueryHandler"/>.
///     All external dependencies are substituted — no I/O involved.
/// </summary>
public class GetSupplierByIdQueryHandlerTests
{
    private readonly ISupplierRepository _supplierRepository = Substitute.For<ISupplierRepository>();

    private readonly ILogger<GetSupplierByIdQueryHandler> _logger =
        Substitute.For<ILogger<GetSupplierByIdQueryHandler>>();

    private readonly GetSupplierByIdQueryHandler _sut;

    public GetSupplierByIdQueryHandlerTests()
    {
        _sut = new GetSupplierByIdQueryHandler(_supplierRepository, _logger);
    }

    // Success paths

    [Fact]
    public async Task Handle_ExistingSupplier_ReturnsSupplier()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var result = await _sut.Handle(new GetSupplierByIdQuery(supplier.Id), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(supplier.Id);
    }

    [Fact]
    public async Task Handle_ExistingSupplier_ReturnsAllFields()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        var result = await _sut.Handle(new GetSupplierByIdQuery(supplier.Id), CancellationToken.None);

        // Assert
        result.LegalName.Should().Be(supplier.LegalName);
        result.CommercialName.Should().Be(supplier.CommercialName);
        result.TaxId.Should().Be(supplier.TaxId);
        result.Country.Should().Be(supplier.Country);
        result.Status.Should().Be(supplier.Status);
        result.RiskLevel.Should().Be(supplier.RiskLevel);
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
            _sut.Handle(new GetSupplierByIdQuery(id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeletedSupplier_ThrowsSupplierNotFoundException()
    {
        // Arrange
        var supplier = SupplierMother.Deleted().Build();
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act & Assert
        await Assert.ThrowsAsync<SupplierNotFoundException>(() =>
            _sut.Handle(new GetSupplierByIdQuery(supplier.Id), CancellationToken.None));
    }
}

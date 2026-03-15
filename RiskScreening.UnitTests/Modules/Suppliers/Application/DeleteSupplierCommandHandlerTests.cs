using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Delete;
using RiskScreening.API.Modules.Suppliers.Domain.Exceptions;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.API.Shared.Domain.Repositories;
using RiskScreening.UnitTests.Modules.Suppliers.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.Suppliers.Application;

/// <summary>
///     Unit tests for <see cref="DeleteSupplierCommandHandler"/>.
///     All external dependencies are substituted — no I/O involved.
/// </summary>
public class DeleteSupplierCommandHandlerTests
{
    private readonly ISupplierRepository _supplierRepository = Substitute.For<ISupplierRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ILogger<DeleteSupplierCommandHandler> _logger =
        Substitute.For<ILogger<DeleteSupplierCommandHandler>>();

    private readonly DeleteSupplierCommandHandler _sut;

    public DeleteSupplierCommandHandlerTests()
    {
        _sut = new DeleteSupplierCommandHandler(_supplierRepository, _unitOfWork, _logger);
    }

    // Success paths

    [Fact]
    public async Task Handle_ExistingSupplier_SoftDeletesAndPersists()
    {
        // Arrange
        var supplier = SupplierMother.Pending().Build();
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        await _sut.Handle(new DeleteSupplierCommand(supplier.Id), CancellationToken.None);

        // Assert
        supplier.IsDeleted.Should().BeTrue();
        _supplierRepository.Received(1).Update(supplier);
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
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
            _sut.Handle(new DeleteSupplierCommand(id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonExistentSupplier_DoesNotPersist()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _supplierRepository.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Supplier?)null);

        // Act
        await Assert.ThrowsAsync<SupplierNotFoundException>(() =>
            _sut.Handle(new DeleteSupplierCommand(id), CancellationToken.None));

        // Assert
        _supplierRepository.DidNotReceive().Update(Arg.Any<Supplier>());
        await _unitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyDeletedSupplier_ThrowsSupplierAlreadyDeletedException()
    {
        // Arrange
        var supplier = SupplierMother.Deleted().Build();
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act & Assert
        await Assert.ThrowsAsync<SupplierAlreadyDeletedException>(() =>
            _sut.Handle(new DeleteSupplierCommand(supplier.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyDeletedSupplier_DoesNotPersist()
    {
        // Arrange
        var supplier = SupplierMother.Deleted().Build();
        _supplierRepository.FindByIdAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        // Act
        await Assert.ThrowsAsync<SupplierAlreadyDeletedException>(() =>
            _sut.Handle(new DeleteSupplierCommand(supplier.Id), CancellationToken.None));

        // Assert
        _supplierRepository.DidNotReceive().Update(Arg.Any<Supplier>());
        await _unitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }
}

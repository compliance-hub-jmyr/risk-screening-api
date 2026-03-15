using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Queries;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Queries;
using RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;
using RiskScreening.UnitTests.Modules.Suppliers.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.Suppliers.Application;

/// <summary>
///     Unit tests for <see cref="GetAllSuppliersQueryHandler"/>.
///     All external dependencies are substituted — no I/O involved.
/// </summary>
public class GetAllSuppliersQueryHandlerTests
{
    private readonly ISupplierRepository _supplierRepository = Substitute.For<ISupplierRepository>();

    private readonly ILogger<GetAllSuppliersQueryHandler> _logger =
        Substitute.For<ILogger<GetAllSuppliersQueryHandler>>();

    private readonly GetAllSuppliersQueryHandler _sut;

    public GetAllSuppliersQueryHandlerTests()
    {
        _sut = new GetAllSuppliersQueryHandler(_supplierRepository, _logger);
    }

    // Success paths

    [Fact]
    public async Task Handle_WithActiveSuppliers_ReturnsPagedResult()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            SupplierMother.Pending().Build(),
            SupplierMother.Pending().Build(),
            SupplierMother.Pending().Build()
        };
        _supplierRepository.Query().Returns(suppliers.AsQueryable().AsEnumerable().BuildMock());

        // Act
        var result = await _sut.Handle(new GetAllSuppliersQuery(), CancellationToken.None);

        // Assert
        result.Content.Should().HaveCount(3);
        result.Page.TotalElements.Should().Be(3);
        result.Page.Number.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithDeletedSuppliers_ExcludesDeletedOnes()
    {
        // Arrange
        var active = SupplierMother.Pending().Build();
        var deleted = SupplierMother.Deleted().Build();
        var suppliers = new List<Supplier> { active, deleted };
        _supplierRepository.Query().Returns(suppliers.AsQueryable().AsEnumerable().BuildMock());

        // Act
        var result = await _sut.Handle(new GetAllSuppliersQuery(), CancellationToken.None);

        // Assert
        result.Content.Should().HaveCount(1);
        result.Content.First().Id.Should().Be(active.Id);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyPage()
    {
        // Arrange
        _supplierRepository.Query().Returns(new List<Supplier>().AsQueryable().AsEnumerable().BuildMock());

        // Act
        var result = await _sut.Handle(new GetAllSuppliersQuery(), CancellationToken.None);

        // Assert
        result.Content.Should().BeEmpty();
        result.Page.TotalElements.Should().Be(0);
        result.Page.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsRequestedPage()
    {
        // Arrange — 5 suppliers, request page 1 with size 2
        var suppliers = Enumerable.Range(0, 5)
            .Select(_ => SupplierMother.Pending().Build())
            .ToList();
        _supplierRepository.Query().Returns(suppliers.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(Page: 1, Size: 2);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content.Should().HaveCount(2);
        result.Page.Number.Should().Be(1);
        result.Page.Size.Should().Be(2);
        result.Page.TotalElements.Should().Be(5);
        result.Page.TotalPages.Should().Be(3);
        result.Page.HasNext.Should().BeTrue();
        result.Page.HasPrevious.Should().BeTrue();
        result.Page.First.Should().BeFalse();
        result.Page.Last.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FirstPage_MetadataIsCorrect()
    {
        // Arrange
        var suppliers = Enumerable.Range(0, 3)
            .Select(_ => SupplierMother.Pending().Build())
            .ToList();
        _supplierRepository.Query().Returns(suppliers.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(Page: 0, Size: 2);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Page.First.Should().BeTrue();
        result.Page.HasPrevious.Should().BeFalse();
        result.Page.HasNext.Should().BeTrue();
        result.Page.Last.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_LastPage_MetadataIsCorrect()
    {
        // Arrange
        var suppliers = Enumerable.Range(0, 3)
            .Select(_ => SupplierMother.Pending().Build())
            .ToList();
        _supplierRepository.Query().Returns(suppliers.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(Page: 1, Size: 2);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Page.Last.Should().BeTrue();
        result.Page.HasNext.Should().BeFalse();
        result.Page.HasPrevious.Should().BeTrue();
    }

    // Failure paths

    [Fact]
    public async Task Handle_FilterByCountry_ReturnsOnlyMatchingSuppliers()
    {
        // Arrange
        var pe = SupplierMother.Pending().WithCountry("PE").Build();
        var us = SupplierMother.Pending().WithCountry("US").Build();
        _supplierRepository.Query().Returns(new List<Supplier> { pe, us }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(Country: "PE");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content.Should().HaveCount(1);
        result.Content.First().Country.Value.Should().Be("PE");
    }

    [Fact]
    public async Task Handle_FilterByStatus_ReturnsOnlyMatchingSuppliers()
    {
        // Arrange
        var pending = SupplierMother.Pending().Build();
        var approved = SupplierMother.Pending().Build();
        approved.Approve();
        _supplierRepository.Query().Returns(new List<Supplier> { pending, approved }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(Status: "Approved");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content.Should().HaveCount(1);
        result.Content.First().Status.Should().Be(SupplierStatus.Approved);
    }

    [Fact]
    public async Task Handle_NoMatchingFilter_ReturnsEmptyPage()
    {
        // Arrange
        var supplier = SupplierMother.Pending().WithCountry("PE").Build();
        _supplierRepository.Query().Returns(new List<Supplier> { supplier }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(Country: "JP");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content.Should().BeEmpty();
        result.Page.TotalElements.Should().Be(0);
    }
}

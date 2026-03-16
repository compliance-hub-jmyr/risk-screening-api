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

    // Sorting

    [Fact]
    public async Task Handle_SortByLegalNameAsc_ReturnsSortedResults()
    {
        // Arrange
        var alpha = SupplierMother.Pending().WithLegalName("Alpha Corp").Build();
        var zeta = SupplierMother.Pending().WithLegalName("Zeta Inc").Build();
        var mid = SupplierMother.Pending().WithLegalName("Mid LLC").Build();
        _supplierRepository.Query().Returns(
            new List<Supplier> { zeta, alpha, mid }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(SortBy: "legalName", SortDirection: "ASC");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content.Should().HaveCount(3);
        result.Content[0].LegalName.Value.Should().Be("Alpha Corp");
        result.Content[1].LegalName.Value.Should().Be("Mid LLC");
        result.Content[2].LegalName.Value.Should().Be("Zeta Inc");
    }

    [Fact]
    public async Task Handle_SortByLegalNameDesc_ReturnsSortedResults()
    {
        // Arrange
        var alpha = SupplierMother.Pending().WithLegalName("Alpha Corp").Build();
        var zeta = SupplierMother.Pending().WithLegalName("Zeta Inc").Build();
        _supplierRepository.Query().Returns(
            new List<Supplier> { alpha, zeta }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(SortBy: "legalName", SortDirection: "DESC");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content[0].LegalName.Value.Should().Be("Zeta Inc");
        result.Content[1].LegalName.Value.Should().Be("Alpha Corp");
    }

    [Fact]
    public async Task Handle_SortByCountryAsc_ReturnsSortedResults()
    {
        // Arrange
        var us = SupplierMother.Pending().WithCountry("US").Build();
        var ar = SupplierMother.Pending().WithCountry("AR").Build();
        var pe = SupplierMother.Pending().WithCountry("PE").Build();
        _supplierRepository.Query().Returns(
            new List<Supplier> { us, ar, pe }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(SortBy: "country", SortDirection: "ASC");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content[0].Country.Value.Should().Be("AR");
        result.Content[1].Country.Value.Should().Be("PE");
        result.Content[2].Country.Value.Should().Be("US");
    }

    [Fact]
    public async Task Handle_SortByStatus_ReturnsSortedResults()
    {
        // Arrange
        var pending = SupplierMother.Pending().Build();
        var approved = SupplierMother.Pending().Build();
        approved.Approve();
        _supplierRepository.Query().Returns(
            new List<Supplier> { approved, pending }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(SortBy: "status", SortDirection: "ASC");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content[0].Status.Should().Be(SupplierStatus.Pending);
        result.Content[1].Status.Should().Be(SupplierStatus.Approved);
    }

    [Fact]
    public async Task Handle_InvalidSortField_FallsBackToDefault()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            SupplierMother.Pending().Build(),
            SupplierMother.Pending().Build()
        };
        _supplierRepository.Query().Returns(suppliers.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(SortBy: "invalidField");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — should not throw, falls back to updatedAt DESC
        result.Content.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NullSortParams_UsesDefaults()
    {
        // Arrange
        var suppliers = new List<Supplier> { SupplierMother.Pending().Build() };
        _supplierRepository.Query().Returns(suppliers.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(SortBy: null, SortDirection: null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — should not throw
        result.Content.Should().HaveCount(1);
    }

    // Filtering — additional cases

    [Fact]
    public async Task Handle_FilterByRiskLevel_ReturnsOnlyMatchingSuppliers()
    {
        // Arrange
        var high = SupplierMother.Pending().Build();
        high.ApplyScreeningResult(RiskLevel.High);
        var none = SupplierMother.Pending().Build();
        _supplierRepository.Query().Returns(
            new List<Supplier> { high, none }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(RiskLevel: "High");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content.Should().HaveCount(1);
        result.Content.First().RiskLevel.Should().Be(RiskLevel.High);
    }

    [Fact]
    public async Task Handle_FilterByStatusCaseInsensitive_ReturnsMatchingSuppliers()
    {
        // Arrange
        var approved = SupplierMother.Pending().Build();
        approved.Approve();
        var pending = SupplierMother.Pending().Build();
        _supplierRepository.Query().Returns(
            new List<Supplier> { approved, pending }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(Status: "approved"); // lowercase

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content.Should().HaveCount(1);
        result.Content.First().Status.Should().Be(SupplierStatus.Approved);
    }

    [Fact]
    public async Task Handle_FilterByInvalidStatus_ReturnsAllSuppliers()
    {
        // Arrange — invalid status string is not a valid enum, so no filter is applied
        var suppliers = new List<Supplier>
        {
            SupplierMother.Pending().Build(),
            SupplierMother.Pending().Build()
        };
        _supplierRepository.Query().Returns(suppliers.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(Status: "InvalidStatus");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — no filter applied, returns all
        result.Content.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_CombinedFilterAndSort_WorksTogether()
    {
        // Arrange
        var peAlpha = SupplierMother.Pending().WithCountry("PE").WithLegalName("Alpha").Build();
        var peZeta = SupplierMother.Pending().WithCountry("PE").WithLegalName("Zeta").Build();
        var usGamma = SupplierMother.Pending().WithCountry("US").WithLegalName("Gamma").Build();
        _supplierRepository.Query().Returns(
            new List<Supplier> { peZeta, usGamma, peAlpha }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(Country: "PE", SortBy: "legalName", SortDirection: "ASC");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content.Should().HaveCount(2);
        result.Content[0].LegalName.Value.Should().Be("Alpha");
        result.Content[1].LegalName.Value.Should().Be("Zeta");
    }

    [Fact]
    public async Task Handle_SortAndPagination_WorksTogether()
    {
        // Arrange — 4 suppliers, sort by legalName ASC, page 1 size 2
        var a = SupplierMother.Pending().WithLegalName("Alpha").Build();
        var b = SupplierMother.Pending().WithLegalName("Beta").Build();
        var c = SupplierMother.Pending().WithLegalName("Charlie").Build();
        var d = SupplierMother.Pending().WithLegalName("Delta").Build();
        _supplierRepository.Query().Returns(
            new List<Supplier> { d, b, a, c }.AsQueryable().AsEnumerable().BuildMock());

        var query = new GetAllSuppliersQuery(SortBy: "legalName", SortDirection: "ASC", Page: 1, Size: 2);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Content.Should().HaveCount(2);
        result.Content[0].LegalName.Value.Should().Be("Charlie");
        result.Content[1].LegalName.Value.Should().Be("Delta");
        result.Page.TotalElements.Should().Be(4);
        result.Page.TotalPages.Should().Be(2);
    }
}

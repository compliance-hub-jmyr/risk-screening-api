using MediatR;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Infrastructure.Persistence.Query.Pagination;
using RiskScreening.API.Shared.Interfaces.REST.Resources;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.Queries;

public record GetAllSuppliersQuery(
    string? LegalName = null,
    string? CommercialName = null,
    string? TaxId = null,
    string? Country = null,
    string? Status = null,
    string? RiskLevel = null,
    int? Page = null,
    int? Size = null,
    string? SortBy = null,
    string? SortDirection = null
) : IRequest<PageResponse<Supplier>>
{
    public PageRequest ToPageRequest() => new(Page, Size, SortBy, SortDirection);
}

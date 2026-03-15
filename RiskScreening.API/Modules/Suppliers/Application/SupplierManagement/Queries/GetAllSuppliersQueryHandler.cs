using MediatR;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Queries;
using RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Query;
using RiskScreening.API.Shared.Infrastructure.Persistence.Query.Pagination;
using RiskScreening.API.Shared.Interfaces.REST.Resources;

namespace RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Queries;

public class GetAllSuppliersQueryHandler(
    ISupplierRepository supplierRepository,
    ILogger<GetAllSuppliersQueryHandler> logger
) : IRequestHandler<GetAllSuppliersQuery, PageResponse<Supplier>>
{
    private static readonly SupplierFilterComposer Filter = new();
    private static readonly SupplierSortConfiguration Sort = new();

    public async Task<PageResponse<Supplier>> Handle(GetAllSuppliersQuery query, CancellationToken ct)
    {
        var baseQuery = supplierRepository
            .Query()
            .Where(s => !s.IsDeleted);

        var filtered = Filter.Apply(baseQuery,
            query.LegalName,
            query.CommercialName,
            query.TaxId,
            query.Country,
            query.Status,
            query.RiskLevel);

        var sorted = Sort.ApplySort(filtered, query.SortBy, query.SortDirection);

        var result = await sorted.ToPageResponseAsync(query.ToPageRequest(), ct);

        logger.LogDebug(
            "Listed suppliers — Page={Page}, Size={Size}, TotalElements={Total}, Filters=[Country={Country}, Status={Status}, RiskLevel={RiskLevel}]",
            query.Page, query.Size, result.Page.TotalElements,
            query.Country, query.Status, query.RiskLevel);

        return result;
    }
}

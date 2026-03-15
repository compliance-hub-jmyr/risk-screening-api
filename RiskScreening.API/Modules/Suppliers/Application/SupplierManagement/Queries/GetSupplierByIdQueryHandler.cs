using MediatR;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Domain.Exceptions;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Queries;

namespace RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Queries;

public class GetSupplierByIdQueryHandler(
    ISupplierRepository supplierRepository,
    ILogger<GetSupplierByIdQueryHandler> logger
) : IRequestHandler<GetSupplierByIdQuery, Supplier>
{
    public async Task<Supplier> Handle(GetSupplierByIdQuery query, CancellationToken ct)
    {
        var supplier = await supplierRepository.FindByIdAsync(query.Id, ct);

        if (supplier is null || supplier.IsDeleted)
        {
            logger.LogWarning("Get-supplier-by-id failed — SupplierId={Id} not found", query.Id);
            throw new SupplierNotFoundException(query.Id);
        }

        logger.LogDebug("Retrieved supplier — SupplierId={Id}, TaxId={TaxId}, Status={Status}",
            supplier.Id, supplier.TaxId.Value, supplier.Status);

        return supplier;
    }
}
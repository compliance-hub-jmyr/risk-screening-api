using MediatR;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Domain.Exceptions;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.API.Shared.Domain.Repositories;

namespace RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Update;

public class UpdateSupplierCommandHandler(
    ISupplierRepository supplierRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateSupplierCommandHandler> logger
) : IRequestHandler<UpdateSupplierCommand, Supplier>
{
    public async Task<Supplier> Handle(UpdateSupplierCommand command, CancellationToken ct)
    {
        var supplier = await supplierRepository.FindByIdAsync(command.Id, ct);

        if (supplier is null || supplier.IsDeleted)
        {
            logger.LogWarning("Update-supplier failed — SupplierId={Id} not found", command.Id);
            throw new SupplierNotFoundException(command.Id);
        }

        if (supplier.TaxId.Value != command.TaxId && await supplierRepository.ExistsByTaxIdAsync(command.TaxId, ct))
        {
            logger.LogWarning("Update-supplier failed — TaxId={TaxId} already exists", command.TaxId);
            throw new SupplierTaxIdAlreadyExistsException(command.TaxId);
        }

        supplier.Update(
            command.LegalName,
            command.CommercialName,
            command.TaxId,
            command.Country,
            command.ContactPhone,
            command.ContactEmail,
            command.Website,
            command.Address,
            command.AnnualBillingUsd,
            command.Notes);

        supplierRepository.Update(supplier);
        await unitOfWork.CompleteAsync(ct);

        logger.LogInformation("Supplier updated with Id={SupplierId}", supplier.Id);

        return supplier;
    }
}
using MediatR;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Domain.Exceptions;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.API.Shared.Domain.Repositories;

namespace RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Create;

public class CreateSupplierCommandHandler(
    ISupplierRepository supplierRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateSupplierCommandHandler> logger
) : IRequestHandler<CreateSupplierCommand, Supplier>
{
    public async Task<Supplier> Handle(CreateSupplierCommand command, CancellationToken ct)
    {
        if (await supplierRepository.ExistsByTaxIdAsync(command.TaxId, ct))
        {
            logger.LogWarning("Create-supplier failed — TaxId={TaxId} already exists", command.TaxId);
            throw new SupplierTaxIdAlreadyExistsException(command.TaxId);
        }

        var supplier = Supplier.Create(
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

        await supplierRepository.AddAsync(supplier);
        await unitOfWork.CompleteAsync(ct);

        logger.LogInformation("Supplier created with Id={SupplierId}", supplier.Id);

        return supplier;
    }
}
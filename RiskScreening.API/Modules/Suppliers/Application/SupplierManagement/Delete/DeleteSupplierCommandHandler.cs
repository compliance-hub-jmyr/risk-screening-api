using MediatR;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Domain.Exceptions;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.API.Shared.Domain.Repositories;

namespace RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Delete;

public class DeleteSupplierCommandHandler(
    ISupplierRepository supplierRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteSupplierCommandHandler> logger
) : IRequestHandler<DeleteSupplierCommand>
{
    public async Task Handle(DeleteSupplierCommand command, CancellationToken ct)
    {
        var supplier = await supplierRepository.FindByIdAsync(command.Id, ct);

        if (supplier is null)
        {
            logger.LogWarning("Delete-supplier failed — SupplierId={Id} not found", command.Id);
            throw new SupplierNotFoundException(command.Id);
        }

        supplier.Delete();
        supplierRepository.Update(supplier);
        await unitOfWork.CompleteAsync(ct);

        logger.LogInformation("Supplier soft-deleted with Id={SupplierId}", supplier.Id);
    }
}
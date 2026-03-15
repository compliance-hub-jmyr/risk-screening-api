using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.Suppliers.Domain.Exceptions;

public class SupplierAlreadyDeletedException(string id) : DomainException($"Supplier '{id}' has already been deleted.",
    ErrorCodes.SupplierAlreadyDeleted,
    ErrorCodes.SupplierAlreadyDeletedCode);
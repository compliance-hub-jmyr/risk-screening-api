using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.Suppliers.Domain.Exceptions;

public class SupplierNotFoundException(string id) : EntityNotFoundException("Supplier", id, ErrorCodes.SupplierNotFound,
    ErrorCodes.SupplierNotFoundCode);
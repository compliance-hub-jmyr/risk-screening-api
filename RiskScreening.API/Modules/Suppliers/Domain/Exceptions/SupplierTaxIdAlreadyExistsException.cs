using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.Suppliers.Domain.Exceptions;

public class SupplierTaxIdAlreadyExistsException(string taxId) : DomainException(
    $"A supplier with tax ID '{taxId}' already exists.",
    ErrorCodes.SupplierTaxIdAlreadyExists,
    ErrorCodes.SupplierTaxIdAlreadyExistsCode);
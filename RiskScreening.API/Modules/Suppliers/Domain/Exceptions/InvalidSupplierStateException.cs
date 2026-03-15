using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.Suppliers.Domain.Exceptions;

public class InvalidSupplierStateException(string id, string reason) : DomainException(
    $"Supplier '{id}' is in an invalid state for this operation: {reason}",
    ErrorCodes.InvalidSupplierState,
    ErrorCodes.InvalidSupplierStateCode);
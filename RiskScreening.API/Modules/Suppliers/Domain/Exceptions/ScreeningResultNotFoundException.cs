using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.Suppliers.Domain.Exceptions;

public class ScreeningResultNotFoundException(string id) : EntityNotFoundException("ScreeningResult", id,
    ErrorCodes.ScreeningResultNotFound, ErrorCodes.ScreeningResultNotFoundCode);
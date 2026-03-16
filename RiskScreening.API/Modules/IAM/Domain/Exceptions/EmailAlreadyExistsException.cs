using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.IAM.Domain.Exceptions;

public class EmailAlreadyExistsException(string email) : BusinessRuleViolationException(
    $"A user with email '{email}' already exists.", ErrorCodes.DuplicateEntry, ErrorCodes.DuplicateEntryCode);
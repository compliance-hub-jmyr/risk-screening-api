using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.IAM.Domain.Exceptions;

public class RoleAlreadyExistsException(string name) : BusinessRuleViolationException($"Role '{name}' already exists.",
    ErrorCodes.DuplicateEntry, ErrorCodes.DuplicateEntryCode);
using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.IAM.Domain.Exceptions;

public class AccountLockedException() :
    AuthenticationException(
        "Account is locked due to too many failed login attempts.", ErrorCodes.AccountLocked,
        ErrorCodes.AccountLockedCode);
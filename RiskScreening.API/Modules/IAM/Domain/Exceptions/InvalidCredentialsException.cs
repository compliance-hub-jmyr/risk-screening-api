using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.IAM.Domain.Exceptions;

public class InvalidCredentialsException() : AuthenticationException("Invalid email or password.",
    ErrorCodes.InvalidCredentials, ErrorCodes.InvalidCredentialsCode);
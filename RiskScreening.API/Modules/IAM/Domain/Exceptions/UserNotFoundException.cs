using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.IAM.Domain.Exceptions;

public class UserNotFoundException : EntityNotFoundException
{
    public UserNotFoundException(string id)
        : base("User", id, ErrorCodes.EntityNotFound, ErrorCodes.EntityNotFoundCode)
    {
    }

    public UserNotFoundException(string field, string value)
        : base("User", field, value, ErrorCodes.EntityNotFound, ErrorCodes.EntityNotFoundCode)
    {
    }
}
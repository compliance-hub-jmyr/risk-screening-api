using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Modules.IAM.Domain.Exceptions;

public class RoleNotFoundException : EntityNotFoundException
{
    public RoleNotFoundException(string name)
        : base("Role", "name", name, ErrorCodes.EntityNotFound, ErrorCodes.EntityNotFoundCode)
    {
    }

    public RoleNotFoundException(string field, string value)
        : base("Role", field, value, ErrorCodes.EntityNotFound, ErrorCodes.EntityNotFoundCode)
    {
    }
}
using MediatR;
using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Domain.Exceptions;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;
using RiskScreening.API.Modules.IAM.Domain.Model.Queries;

namespace RiskScreening.API.Modules.IAM.Application.Authentication;

public class GetCurrentUserQueryHandler(
    IUserRepository userRepository
) : IRequestHandler<GetCurrentUserQuery, User>
{
    public async Task<User> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        return await userRepository.FindByEmailAsync(query.Email, ct)
               ?? throw new UserNotFoundException("email", query.Email);
    }
}
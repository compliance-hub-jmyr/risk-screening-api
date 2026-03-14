using MediatR;
using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Domain.Exceptions;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;
using RiskScreening.API.Modules.IAM.Domain.Model.Queries;

namespace RiskScreening.API.Modules.IAM.Application.Authentication;

public class GetCurrentUserQueryHandler(
    IUserRepository userRepository,
    ILogger<GetCurrentUserQueryHandler> logger
) : IRequestHandler<GetCurrentUserQuery, User>
{
    public async Task<User> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var user = await userRepository.FindByEmailAsync(query.Email, ct);

        if (user is null)
        {
            logger.LogWarning("Get-current-user failed — user not found for Email={Email}", query.Email);
            throw new UserNotFoundException("email", query.Email);
        }

        logger.LogInformation("Get-current-user succeeded for UserId={UserId}", user.Id);

        return user;
    }
}
using MediatR;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;

namespace RiskScreening.API.Modules.IAM.Domain.Model.Queries;

public record GetCurrentUserQuery(string Email) : IRequest<User>;
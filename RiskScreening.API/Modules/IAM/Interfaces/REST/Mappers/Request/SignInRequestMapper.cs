using RiskScreening.API.Modules.IAM.Domain.Model.Commands;
using RiskScreening.API.Modules.IAM.Interfaces.REST.Resources.Requests;

namespace RiskScreening.API.Modules.IAM.Interfaces.REST.Mappers.Request;

/// <summary>Maps a <see cref="SignInRequest"/> DTO to a <see cref="SignInCommand"/>.</summary>
public static class SignInRequestMapper
{
    public static SignInCommand ToCommand(SignInRequest request)
    {
        return new SignInCommand(request.Email, request.Password);
    }
}
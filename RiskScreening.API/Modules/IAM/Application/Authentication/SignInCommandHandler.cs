using MediatR;
using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Application.Results;
using RiskScreening.API.Modules.IAM.Domain.Exceptions;
using RiskScreening.API.Modules.IAM.Domain.Model.Commands;
using RiskScreening.API.Shared.Domain.Repositories;

namespace RiskScreening.API.Modules.IAM.Application.Authentication;

/// <summary>
/// Handles the <see cref="SignInCommand"/> by validating the user's credentials,
/// recording the login attempt result, and issuing a JWT token upon success.
/// </summary>
/// <remarks>
/// This handler follows the application layer pattern in the IAM module.
/// It delegates persistence to <see cref="IUserRepository"/> and <see cref="IUnitOfWork"/>,
/// password verification to <see cref="IPasswordHasher"/>, and token generation to
/// <see cref="IJwtTokenService"/>.
/// </remarks>
public class SignInCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork,
    ILogger<SignInCommandHandler> logger
) : IRequestHandler<SignInCommand, SignInResult>
{
    /// <summary>
    /// Processes the sign-in command asynchronously.
    /// </summary>
    /// <param name="command">The sign-in command containing the user's email and password.</param>
    /// <param name="ct">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="SignInResult"/> containing the authenticated user and the generated JWT token.
    /// </returns>
    /// <exception cref="InvalidCredentialsException">
    /// Thrown when the email is not found or the password does not match.
    /// Also thrown when the account is in a state that prevents login (e.g., suspended).
    /// </exception>
    public async Task<SignInResult> Handle(SignInCommand command, CancellationToken ct)
    {
        logger.LogInformation("Sign-in attempt for Email={Email}", command.Email);

        var user = await userRepository.FindByEmailAsync(command.Email, ct);

        if (user is null)
        {
            logger.LogWarning("Sign-in failed — user not found for Email={Email}", command.Email);
            throw new InvalidCredentialsException();
        }

        user.EnsureCanLogin();

        if (!passwordHasher.Verify(command.Password, user.Password.Hash))
        {
            user.RecordFailedLogin();
            userRepository.Update(user);
            await unitOfWork.CompleteAsync(ct);

            logger.LogWarning(
                "Sign-in failed — invalid password for UserId={UserId}, FailedAttempts={FailedAttempts}",
                user.Id,
                user.FailedLoginAttempts);

            throw new InvalidCredentialsException();
        }

        user.RecordSuccessfulLogin();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(ct);

        var token = jwtTokenService.GenerateToken(user);

        logger.LogInformation("Sign-in succeeded for UserId={UserId}", user.Id);

        return new SignInResult(user, token);
    }
}
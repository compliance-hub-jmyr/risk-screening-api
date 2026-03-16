using AwesomeAssertions;
using Bogus;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiskScreening.API.Modules.IAM.Application.Authentication;
using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Domain.Exceptions;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Domain.Repositories;
using RiskScreening.UnitTests.Modules.IAM.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.IAM.Application;

/// <summary>
///     Unit tests for <see cref="SignInCommandHandler"/>.
///     All external dependencies are substituted — no I/O involved.
/// </summary>
public class SignInCommandHandlerTests
{
    private static readonly Faker Faker = new();

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<SignInCommandHandler> _logger = Substitute.For<ILogger<SignInCommandHandler>>();

    private readonly SignInCommandHandler _sut;

    public SignInCommandHandlerTests()
    {
        _sut = new SignInCommandHandler(_userRepository, _passwordHasher, _jwtTokenService, _unitOfWork, _logger);
    }

    // Success paths

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenAndPersistsLogin()
    {
        // Arrange
        var user = UserMother.Active().Build();
        var command = SignInCommandMother.ValidFor(user.Email.Value);
        var expectedToken = Faker.Random.AlphaNumeric(40);

        _userRepository.FindByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.Password, user.Password.Hash).Returns(true);
        _jwtTokenService.GenerateToken(user).Returns(expectedToken);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Token.Should().Be(expectedToken);
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    // Failure paths 

    [Fact]
    public async Task Handle_UserNotFound_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var command = SignInCommandMother.WithNonExistentUser();

        _userRepository.FindByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var user = UserMother.Active().Build();
        var command = SignInCommandMother.WithWrongPassword(user.Email.Value);

        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.Password, user.Password.Hash).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WrongPassword_IncrementsFailedLoginAttempts()
    {
        // Arrange
        var user = UserMother.Active().Build();
        var command = SignInCommandMother.WithWrongPassword(user.Email.Value);

        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        // Act
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.Handle(command, CancellationToken.None));

        // Assert — domain side effect
        user.FailedLoginAttempts.Should().Be(1);
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_LockedAccount_ThrowsAccountLockedException()
    {
        // Arrange
        var user = UserMother.Locked().Build();
        var command = SignInCommandMother.ValidFor(user.Email.Value);

        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        // Act & Assert
        await Assert.ThrowsAsync<AccountLockedException>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SuspendedAccount_ThrowsAccountLockedException()
    {
        // Arrange
        var user = UserMother.Suspended().Build();
        var command = SignInCommandMother.ValidFor(user.Email.Value);

        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        // Act & Assert
        await Assert.ThrowsAsync<AccountLockedException>(() => _sut.Handle(command, CancellationToken.None));
    }
}
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiskScreening.API.Modules.IAM.Application.Authentication;
using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Domain.Exceptions;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;
using RiskScreening.UnitTests.Modules.IAM.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.IAM.Application;

/// <summary>
///     Unit tests for <see cref="GetCurrentUserQueryHandler"/>.
///     All external dependencies are substituted — no I/O involved.
/// </summary>
public class GetCurrentUserQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ILogger<GetCurrentUserQueryHandler> _logger = Substitute.For<ILogger<GetCurrentUserQueryHandler>>();

    private readonly GetCurrentUserQueryHandler _sut;

    public GetCurrentUserQueryHandlerTests()
    {
        _sut = new GetCurrentUserQueryHandler(_userRepository, _logger);
    }

    // Success paths

    [Fact]
    public async Task Handle_ExistingUser_ReturnsUserAndQueriesByEmail()
    {
        // Arrange
        var user = UserMother.Active().Build();
        var query = GetCurrentUserQueryMother.ForExistingUser(user.Email.Value);

        _userRepository.FindByEmailAsync(query.Email, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Email.Value.Should().Be(user.Email.Value);
        await _userRepository.Received(1).FindByEmailAsync(query.Email, Arg.Any<CancellationToken>());
    }

    // Failure paths

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        var query = GetCurrentUserQueryMother.ForNonExistentUser();

        _userRepository.FindByEmailAsync(query.Email, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserNotFound_DoesNotReturnPartialResult()
    {
        // Arrange
        var query = GetCurrentUserQueryMother.ForNonExistentUser();

        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var exception = await Record.ExceptionAsync(() => _sut.Handle(query, CancellationToken.None));

        // Assert
        exception.Should().BeOfType<UserNotFoundException>();
    }
}

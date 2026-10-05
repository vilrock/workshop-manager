using Domain.Abstractions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using NSubstitute;
using Service.Queries.Login;
using UnitTests.Support;

namespace UnitTests.Handlers;

public sealed class LoginHandlerTests
{
    private readonly IUserRepository users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService tokens = Substitute.For<ITokenService>();

    private LoginHandler CreateHandler() => new(users, hasher, tokens);

    [Fact]
    public async Task Valid_credentials_return_a_token_and_the_email_is_normalized()
    {
        var user = Builders.UserWithRole(UserRole.Advisor);
        users.GetByEmailAsync("advisor@example.com", Arg.Any<CancellationToken>()).Returns(user);
        hasher.Verify("secret", user.PasswordHash).Returns(true);
        tokens.Create(user).Returns(new AccessToken("jwt", FixedClock.Default.UtcNow.AddHours(1)));

        var result = await CreateHandler().Handle(new LoginQuery("  Advisor@Example.com ", "secret"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt", result.Value.Token.Value);
    }

    [Fact]
    public async Task Unknown_email_and_wrong_password_return_the_same_error()
    {
        var user = Builders.UserWithRole(UserRole.Admin);
        users.GetByEmailAsync("admin@example.com", Arg.Any<CancellationToken>()).Returns(user);
        users.GetByEmailAsync("ghost@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var wrongPassword = await CreateHandler().Handle(new LoginQuery("admin@example.com", "bad"), CancellationToken.None);
        var unknownEmail = await CreateHandler().Handle(new LoginQuery("ghost@example.com", "bad"), CancellationToken.None);

        Assert.Equal(DomainErrors.Auth.InvalidCredentials, wrongPassword.Error);
        Assert.Equal(DomainErrors.Auth.InvalidCredentials, unknownEmail.Error);
        tokens.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Fact]
    public async Task Disabled_accounts_cannot_sign_in()
    {
        var user = Builders.UserWithRole(UserRole.Mechanic, isActive: false);
        users.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await CreateHandler().Handle(new LoginQuery(user.Email, "secret"), CancellationToken.None);

        Assert.Equal(DomainErrors.Auth.InactiveUser, result.Error);
    }
}

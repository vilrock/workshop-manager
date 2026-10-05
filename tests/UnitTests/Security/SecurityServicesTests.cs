using System.IdentityModel.Tokens.Jwt;
using Domain.Enums;
using Microsoft.Extensions.Options;
using UnitTests.Support;
using Util.Security;

namespace UnitTests.Security;

public sealed class SecurityServicesTests
{
    private readonly Pbkdf2PasswordHasher hasher = new();

    [Fact]
    public void A_hashed_password_verifies_and_uses_a_random_salt()
    {
        var first = hasher.Hash("Workshop#2026");
        var second = hasher.Hash("Workshop#2026");

        Assert.True(hasher.Verify("Workshop#2026", first));
        Assert.NotEqual(first, second);
        Assert.StartsWith("PBKDF2-SHA256$", first);
    }

    [Theory]
    [InlineData("wrong-password")]
    [InlineData("")]
    public void A_different_password_does_not_verify(string candidate)
    {
        Assert.False(hasher.Verify(candidate, hasher.Hash("Workshop#2026")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("PBKDF2-SHA256$abc$salt$hash")]
    [InlineData("PBKDF2-SHA256$1000$not-base64!$also-not")]
    [InlineData("OTHER$1000$AAAA$AAAA")]
    public void Malformed_hashes_never_verify(string stored)
    {
        Assert.False(hasher.Verify("anything", stored));
    }

    [Fact]
    public void The_seeded_demo_hash_format_is_accepted()
    {
        const string seeded = "PBKDF2-SHA256$310000$bAYh62rTBKidZMHlPwX1Hw==$R4F8LbzdJUD7k3Ob4VB5TzZksftBSJQdy9+pFd/Wq28=";

        Assert.True(hasher.Verify("Workshop#2026", seeded));
    }

    [Fact]
    public void Tokens_carry_subject_role_and_expiry()
    {
        var options = Options.Create(new JwtOptions { Secret = new string('s', 40), Issuer = "issuer", Audience = "audience", ExpiresMinutes = 30 });
        var service = new JwtTokenService(options, FixedClock.Default);
        var user = Builders.UserWithRole(UserRole.Mechanic);

        var token = service.Create(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        Assert.Equal(FixedClock.Default.UtcNow.AddMinutes(30), token.ExpiresAtUtc);
        Assert.Equal(user.UserId.ToString(), parsed.Subject);
        Assert.Equal("Mechanic", parsed.Claims.Single(claim => claim.Type == JwtClaimTypes.Role).Value);
        Assert.Equal("issuer", parsed.Issuer);
    }

    [Theory]
    [InlineData("short", "issuer", "audience", false)]
    [InlineData("0123456789012345678901234567890123456789", "", "audience", false)]
    [InlineData("0123456789012345678901234567890123456789", "issuer", "audience", true)]
    public void Jwt_options_are_validated_at_startup(string secret, string issuer, string audience, bool valid)
    {
        var result = new JwtOptionsValidator().Validate(null, new JwtOptions { Secret = secret, Issuer = issuer, Audience = audience });

        Assert.Equal(valid, result.Succeeded);
    }
}

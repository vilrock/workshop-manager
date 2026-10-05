using Domain.Models;

namespace Domain.Abstractions;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessToken Create(User user);
}

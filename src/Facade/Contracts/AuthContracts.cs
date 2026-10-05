using Domain.Enums;

namespace Facade.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record UserResponse(Guid UserId, string FullName, string Email, UserRole Role);

public sealed record MechanicResponse(Guid UserId, string FullName);

public sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, UserResponse User);

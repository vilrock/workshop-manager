using Domain.Enums;

namespace Domain.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid UserId { get; }

    string Email { get; }

    UserRole Role { get; }

    string CorrelationId { get; }
}

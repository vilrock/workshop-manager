using Domain.Models;

namespace Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> ListActiveMechanicsAsync(CancellationToken cancellationToken);
}

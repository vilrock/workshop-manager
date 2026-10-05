using Domain.Models;
using Domain.Repositories;

namespace Data.Postgres.Repositories;

public sealed class UserRepository(PostgresSession session) : IUserRepository
{
    private const string SelectColumns = "SELECT user_id, email, full_name, role, password_hash, is_active, created_at AS created_at_utc FROM users";

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        session.QuerySingleOrDefaultAsync<User>($"{SelectColumns} WHERE lower(email) = @Email", new { Email = email }, cancellationToken);

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        session.QuerySingleOrDefaultAsync<User>($"{SelectColumns} WHERE user_id = @UserId", new { UserId = userId }, cancellationToken);

    public Task<IReadOnlyList<User>> ListActiveMechanicsAsync(CancellationToken cancellationToken) =>
        session.QueryAsync<User>($"{SelectColumns} WHERE role = 'Mechanic' AND is_active ORDER BY full_name", null, cancellationToken);
}

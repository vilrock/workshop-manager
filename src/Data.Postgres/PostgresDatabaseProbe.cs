using Dapper;
using Domain.Abstractions;
using Npgsql;

namespace Data.Postgres;

public sealed class PostgresDatabaseProbe(NpgsqlDataSource dataSource) : IDatabaseProbe
{
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            return await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", commandTimeout: 5, cancellationToken: cancellationToken)) == 1;
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return false;
        }
    }
}

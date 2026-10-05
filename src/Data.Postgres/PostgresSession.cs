using System.Data.Common;
using Dapper;
using Domain.Abstractions;
using Domain.Common;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;
using Polly.Retry;

namespace Data.Postgres;

public sealed class PostgresSession : IUnitOfWork, IAsyncDisposable
{
    private const int MaxApplicationNameLength = 63;

    private static readonly ResiliencePipeline ConnectionPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>(exception => exception.IsTransient).Handle<TimeoutException>()
        })
        .Build();

    private readonly NpgsqlDataSource dataSource;
    private readonly ICurrentUser currentUser;
    private readonly int commandTimeoutSeconds;
    private NpgsqlConnection? connection;
    private NpgsqlTransaction? transaction;

    static PostgresSession()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public PostgresSession(NpgsqlDataSource dataSource, IOptions<PostgresOptions> options, ICurrentUser currentUser)
    {
        this.dataSource = dataSource;
        this.currentUser = currentUser;
        commandTimeoutSeconds = options.Value.CommandTimeoutSeconds;
    }

    public bool HasActiveTransaction => transaction is not null;

    public async Task BeginAsync(CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active for this scope.");
        }

        var openConnection = await GetConnectionAsync(cancellationToken);
        transaction = await openConnection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        var active = transaction ?? throw new InvalidOperationException("There is no active transaction to commit.");
        transaction = null;
        await using (active)
        {
            await active.CommitAsync(cancellationToken);
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        var active = transaction;
        if (active is null)
        {
            return;
        }

        transaction = null;
        await using (active)
        {
            await active.RollbackAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        var openConnection = await GetConnectionAsync(cancellationToken);
        var rows = await openConnection.QueryAsync<T>(CreateCommand(sql, parameters, cancellationToken));
        return rows.AsList();
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        var openConnection = await GetConnectionAsync(cancellationToken);
        return await openConnection.QuerySingleOrDefaultAsync<T>(CreateCommand(sql, parameters, cancellationToken));
    }

    public async Task<T> ExecuteScalarAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        var openConnection = await GetConnectionAsync(cancellationToken);
        return (await openConnection.ExecuteScalarAsync<T>(CreateCommand(sql, parameters, cancellationToken)))!;
    }

    public async Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object? parameters, CancellationToken cancellationToken)
    {
        var openConnection = await GetConnectionAsync(cancellationToken);
        return await openConnection.QueryMultipleAsync(CreateCommand(sql, parameters, cancellationToken));
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (transaction is null)
        {
            throw new InvalidOperationException("Mutating statements must run inside a transaction.");
        }

        var openConnection = await GetConnectionAsync(cancellationToken);

        try
        {
            return await openConnection.ExecuteAsync(CreateCommand(sql, parameters, cancellationToken));
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateRecordException(exception.ConstraintName ?? "unknown");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (transaction is not null)
        {
            await transaction.DisposeAsync();
            transaction = null;
        }

        if (connection is not null)
        {
            await connection.DisposeAsync();
            connection = null;
        }
    }

    private async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (connection is not null)
        {
            return connection;
        }

        var opened = await ConnectionPipeline.ExecuteAsync(async token => await dataSource.OpenConnectionAsync(token), cancellationToken);
        await TagWithCorrelationIdAsync(opened, cancellationToken);
        connection = opened;
        return opened;
    }

    private async Task TagWithCorrelationIdAsync(DbConnection openedConnection, CancellationToken cancellationToken)
    {
        var applicationName = $"workshop-api:{currentUser.CorrelationId}";
        if (applicationName.Length > MaxApplicationNameLength)
        {
            applicationName = applicationName[..MaxApplicationNameLength];
        }

        var command = new CommandDefinition("SELECT set_config('application_name', @ApplicationName, false)", new { ApplicationName = applicationName }, commandTimeout: commandTimeoutSeconds, cancellationToken: cancellationToken);
        await openedConnection.ExecuteScalarAsync<string>(command);
    }

    private CommandDefinition CreateCommand(string sql, object? parameters, CancellationToken cancellationToken) =>
        new(sql, parameters, transaction, commandTimeoutSeconds, cancellationToken: cancellationToken);
}

using Domain.Models;
using Domain.Repositories;

namespace Data.Postgres.Repositories;

public sealed class IdempotencyRepository(PostgresSession session) : IIdempotencyRepository
{
    public async Task<bool> TryRegisterAsync(string key, Guid userId, string requestHash, DateTime nowUtc, CancellationToken cancellationToken) =>
        await session.ExecuteAsync(
            """
            INSERT INTO idempotency_keys (idempotency_key, user_id, request_hash, created_at)
            VALUES (@Key, @UserId, @RequestHash, @NowUtc)
            ON CONFLICT DO NOTHING
            """,
            new { Key = key, UserId = userId, RequestHash = requestHash, NowUtc = nowUtc },
            cancellationToken) == 1;

    public async Task<IdempotencyRecord?> GetAsync(string key, Guid userId, CancellationToken cancellationToken)
    {
        var row = await session.QuerySingleOrDefaultAsync<IdempotencyRow>(
            "SELECT request_hash, work_order_id FROM idempotency_keys WHERE idempotency_key = @Key AND user_id = @UserId",
            new { Key = key, UserId = userId },
            cancellationToken);

        return row is null ? null : new IdempotencyRecord(row.RequestHash, row.WorkOrderId);
    }

    public async Task AttachWorkOrderAsync(string key, Guid userId, Guid workOrderId, CancellationToken cancellationToken) =>
        await session.ExecuteAsync(
            "UPDATE idempotency_keys SET work_order_id = @WorkOrderId WHERE idempotency_key = @Key AND user_id = @UserId",
            new { Key = key, UserId = userId, WorkOrderId = workOrderId },
            cancellationToken);

    private sealed class IdempotencyRow
    {
        public string RequestHash { get; init; } = string.Empty;

        public Guid? WorkOrderId { get; init; }
    }
}

using Domain.Models;

namespace Domain.Repositories;

public interface IIdempotencyRepository
{
    Task<bool> TryRegisterAsync(string key, Guid userId, string requestHash, DateTime nowUtc, CancellationToken cancellationToken);

    Task<IdempotencyRecord?> GetAsync(string key, Guid userId, CancellationToken cancellationToken);

    Task AttachWorkOrderAsync(string key, Guid userId, Guid workOrderId, CancellationToken cancellationToken);
}

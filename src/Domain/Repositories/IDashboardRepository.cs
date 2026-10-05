using Domain.Models;

namespace Domain.Repositories;

public interface IDashboardRepository
{
    Task<DashboardStats> GetStatsAsync(Guid? mechanicId, DateTime nowUtc, CancellationToken cancellationToken);
}

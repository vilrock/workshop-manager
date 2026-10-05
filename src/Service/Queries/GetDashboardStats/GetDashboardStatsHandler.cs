using Domain.Abstractions;
using Domain.Common;
using Domain.Enums;
using Domain.Repositories;
using MediatR;
using Service.Common;

namespace Service.Queries.GetDashboardStats;

public sealed class GetDashboardStatsHandler(ICurrentUser currentUser, IClock clock, IDashboardRepository dashboard) : IRequestHandler<GetDashboardStatsQuery, Result<DashboardView>>
{
    public async Task<Result<DashboardView>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var mechanicScope = currentUser.Role == UserRole.Mechanic ? currentUser.UserId : (Guid?)null;
        var stats = await dashboard.GetStatsAsync(mechanicScope, clock.UtcNow, cancellationToken);
        var recent = stats.RecentOrders.Select(summary => WorkOrderViewFactory.ToSummaryView(summary, currentUser)).ToList();

        return new DashboardView(stats, recent);
    }
}

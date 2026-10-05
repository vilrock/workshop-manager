using Domain.Common;
using Domain.Models;
using MediatR;
using Service.Common;

namespace Service.Queries.GetDashboardStats;

public sealed record GetDashboardStatsQuery : IRequest<Result<DashboardView>>;

public sealed record DashboardView(DashboardStats Stats, IReadOnlyList<WorkOrderSummaryView> RecentOrders);

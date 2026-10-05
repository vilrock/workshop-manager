using Domain.Enums;

namespace Facade.Contracts;

public sealed record StatusCountResponse(WorkOrderStatus Status, int Count);

public sealed record MonthRevenueResponse(string Month, decimal Revenue);

public sealed record DashboardStatsResponse(
    IReadOnlyList<StatusCountResponse> OrdersByStatus,
    int OpenOrders,
    int CompletedThisMonth,
    decimal MonthlyRevenue,
    double? AverageHoursToComplete,
    double? AverageHoursToDiagnose,
    IReadOnlyList<MonthRevenueResponse> RevenueByMonth,
    IReadOnlyList<WorkOrderSummaryResponse> RecentOrders);

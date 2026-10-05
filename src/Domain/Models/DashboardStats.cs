using Domain.Enums;

namespace Domain.Models;

public sealed record StatusCount(WorkOrderStatus Status, int Count);

public sealed record MonthRevenue(string Month, decimal Revenue);

public sealed record DashboardStats(
    IReadOnlyList<StatusCount> OrdersByStatus,
    int OpenOrders,
    int CompletedThisMonth,
    decimal MonthlyRevenue,
    double? AverageHoursToComplete,
    double? AverageHoursToDiagnose,
    IReadOnlyList<MonthRevenue> RevenueByMonth,
    IReadOnlyList<WorkOrderSummary> RecentOrders);

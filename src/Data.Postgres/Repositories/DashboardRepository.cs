using Dapper;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;

namespace Data.Postgres.Repositories;

public sealed class DashboardRepository(PostgresSession session) : IDashboardRepository
{
    private const int RevenueMonths = 6;

    private const int RecentOrderCount = 6;

    private const string SummaryColumns = """
        SELECT w.work_order_id, w.order_number, w.status, w.customer_id, c.full_name AS customer_name, w.vehicle_id,
            v.year || ' ' || v.make || ' ' || v.model || ' (' || v.license_plate || ')' AS vehicle_description,
            w.description, w.assigned_mechanic_id, m.full_name AS assigned_mechanic_name, w.total_amount, w.created_at AS created_at_utc, w.updated_at AS updated_at_utc
        FROM work_orders w
        JOIN customers c ON c.customer_id = w.customer_id
        JOIN vehicles v ON v.vehicle_id = w.vehicle_id
        LEFT JOIN users m ON m.user_id = w.assigned_mechanic_id
        """;

    public async Task<DashboardStats> GetStatsAsync(Guid? mechanicId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var revenueWindowStart = monthStart.AddMonths(-(RevenueMonths - 1));
        var parameters = new { MechanicId = mechanicId, MonthStart = monthStart, RevenueWindowStart = revenueWindowStart, RecentOrderCount };

        var sql = $"""
            SELECT status, count(*)::int AS count
            FROM work_orders
            WHERE (@MechanicId::uuid IS NULL OR assigned_mechanic_id = @MechanicId)
            GROUP BY status;

            SELECT count(*)::int AS completed_count, COALESCE(sum(total_amount), 0) AS revenue
            FROM work_orders
            WHERE completed_at >= @MonthStart AND status IN ('Completed', 'Delivered')
                AND (@MechanicId::uuid IS NULL OR assigned_mechanic_id = @MechanicId);

            SELECT avg(extract(epoch FROM (completed_at - created_at)) / 3600.0)::float8
            FROM work_orders
            WHERE completed_at IS NOT NULL
                AND (@MechanicId::uuid IS NULL OR assigned_mechanic_id = @MechanicId);

            SELECT avg(extract(epoch FROM (h.changed_at - w.created_at)) / 3600.0)::float8
            FROM work_orders w
            JOIN work_order_history h ON h.work_order_id = w.work_order_id AND h.to_status = 'Diagnosed'
            WHERE (@MechanicId::uuid IS NULL OR w.assigned_mechanic_id = @MechanicId);

            SELECT to_char(date_trunc('month', completed_at AT TIME ZONE 'UTC'), 'YYYY-MM') AS month, COALESCE(sum(total_amount), 0) AS revenue
            FROM work_orders
            WHERE completed_at >= @RevenueWindowStart AND status IN ('Completed', 'Delivered')
                AND (@MechanicId::uuid IS NULL OR assigned_mechanic_id = @MechanicId)
            GROUP BY 1;

            {SummaryColumns}
            WHERE (@MechanicId::uuid IS NULL OR w.assigned_mechanic_id = @MechanicId)
            ORDER BY w.updated_at DESC, w.order_number DESC
            LIMIT @RecentOrderCount;
            """;

        using var grid = await session.QueryMultipleAsync(sql, parameters, cancellationToken);
        var statusRows = (await grid.ReadAsync<StatusCountRow>()).ToList();
        var monthRow = await grid.ReadSingleAsync<MonthRow>();
        var averageHoursToComplete = await grid.ReadSingleAsync<double?>();
        var averageHoursToDiagnose = await grid.ReadSingleAsync<double?>();
        var revenueRows = (await grid.ReadAsync<RevenueRow>()).ToList();
        var recentOrders = (await grid.ReadAsync<WorkOrderSummary>()).AsList();

        var ordersByStatus = Enum.GetValues<WorkOrderStatus>()
            .Select(status => new StatusCount(status, statusRows.FirstOrDefault(row => row.Status == status)?.Count ?? 0))
            .ToList();

        return new DashboardStats(
            ordersByStatus,
            CountOpenOrders(ordersByStatus),
            monthRow.CompletedCount,
            monthRow.Revenue,
            averageHoursToComplete,
            averageHoursToDiagnose,
            BuildRevenueByMonth(revenueRows, revenueWindowStart),
            recentOrders);
    }

    private static int CountOpenOrders(IEnumerable<StatusCount> ordersByStatus) =>
        ordersByStatus
            .Where(entry => entry.Status is WorkOrderStatus.Received or WorkOrderStatus.Diagnosed or WorkOrderStatus.Approved or WorkOrderStatus.InProgress)
            .Sum(entry => entry.Count);

    private static List<MonthRevenue> BuildRevenueByMonth(IReadOnlyCollection<RevenueRow> rows, DateTime windowStart) =>
        Enumerable.Range(0, RevenueMonths)
            .Select(offset => windowStart.AddMonths(offset).ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture))
            .Select(month => new MonthRevenue(month, rows.FirstOrDefault(row => row.Month == month)?.Revenue ?? 0m))
            .ToList();

    private sealed class StatusCountRow
    {
        public WorkOrderStatus Status { get; init; }

        public int Count { get; init; }
    }

    private sealed class MonthRow
    {
        public int CompletedCount { get; init; }

        public decimal Revenue { get; init; }
    }

    private sealed class RevenueRow
    {
        public string Month { get; init; } = string.Empty;

        public decimal Revenue { get; init; }
    }
}

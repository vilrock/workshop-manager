using Dapper;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;

namespace Data.Postgres.Repositories;

public sealed class WorkOrderRepository(PostgresSession session) : IWorkOrderRepository
{
    private const string SummaryColumns = """
        SELECT w.work_order_id, w.order_number, w.status, w.customer_id, c.full_name AS customer_name, w.vehicle_id,
            v.year || ' ' || v.make || ' ' || v.model || ' (' || v.license_plate || ')' AS vehicle_description,
            w.description, w.assigned_mechanic_id, m.full_name AS assigned_mechanic_name, w.total_amount, w.created_at AS created_at_utc, w.updated_at AS updated_at_utc
        FROM work_orders w
        JOIN customers c ON c.customer_id = w.customer_id
        JOIN vehicles v ON v.vehicle_id = w.vehicle_id
        LEFT JOIN users m ON m.user_id = w.assigned_mechanic_id
        """;

    private const string ListFilter = """
        (@Status::text IS NULL OR w.status = @Status)
        AND (@MechanicId::uuid IS NULL OR w.assigned_mechanic_id = @MechanicId)
        AND (@Pattern::text IS NULL
            OR c.full_name ILIKE @Pattern ESCAPE '!'
            OR v.license_plate ILIKE @Pattern ESCAPE '!'
            OR w.description ILIKE @Pattern ESCAPE '!'
            OR 'WO-' || w.order_number ILIKE @Pattern ESCAPE '!')
        """;

    public async Task<PagedResult<WorkOrderSummary>> ListAsync(WorkOrderFilter filter, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            Status = filter.Status?.ToString(),
            filter.MechanicId,
            Pattern = LikePattern.FromSearch(filter.Search),
            filter.PageSize,
            Offset = (filter.Page - 1) * filter.PageSize
        };

        var sql = $"""
            SELECT count(*)::int
            FROM work_orders w
            JOIN customers c ON c.customer_id = w.customer_id
            JOIN vehicles v ON v.vehicle_id = w.vehicle_id
            WHERE {ListFilter};
            {SummaryColumns} WHERE {ListFilter} ORDER BY w.created_at DESC, w.order_number DESC LIMIT @PageSize OFFSET @Offset;
            """;

        using var grid = await session.QueryMultipleAsync(sql, parameters, cancellationToken);
        var totalCount = await grid.ReadSingleAsync<int>();
        var items = (await grid.ReadAsync<WorkOrderSummary>()).AsList();
        return new PagedResult<WorkOrderSummary>(items, filter.Page, filter.PageSize, totalCount);
    }

    public async Task<WorkOrderDetail?> GetDetailAsync(Guid workOrderId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT w.work_order_id, w.order_number, w.status, w.customer_id, c.full_name AS customer_name, w.vehicle_id,
                v.year || ' ' || v.make || ' ' || v.model || ' (' || v.license_plate || ')' AS vehicle_description,
                w.description, w.assigned_mechanic_id, m.full_name AS assigned_mechanic_name, w.total_amount, w.created_at AS created_at_utc, w.updated_at AS updated_at_utc,
                w.mileage_km, creator.full_name AS created_by_name, w.completed_at AS completed_at_utc
            FROM work_orders w
            JOIN customers c ON c.customer_id = w.customer_id
            JOIN vehicles v ON v.vehicle_id = w.vehicle_id
            JOIN users creator ON creator.user_id = w.created_by_user_id
            LEFT JOIN users m ON m.user_id = w.assigned_mechanic_id
            WHERE w.work_order_id = @WorkOrderId;

            SELECT line_item_id, work_order_id, item_type, description, quantity, unit_price, line_total, added_by_user_id, created_at AS created_at_utc
            FROM work_order_items
            WHERE work_order_id = @WorkOrderId
            ORDER BY created_at, line_item_id;

            SELECT h.history_id, h.work_order_id, h.from_status, h.to_status, h.changed_by_user_id, u.full_name AS changed_by_name, h.changed_at AS changed_at_utc, h.correlation_id, h.note
            FROM work_order_history h
            JOIN users u ON u.user_id = h.changed_by_user_id
            WHERE h.work_order_id = @WorkOrderId
            ORDER BY h.changed_at, h.history_id;
            """;

        using var grid = await session.QueryMultipleAsync(sql, new { WorkOrderId = workOrderId }, cancellationToken);
        var detail = await grid.ReadSingleOrDefaultAsync<WorkOrderDetail>();
        if (detail is null)
        {
            return null;
        }

        detail.Items = (await grid.ReadAsync<WorkOrderLineItem>()).AsList();
        detail.History = (await grid.ReadAsync<WorkOrderHistoryEntry>()).AsList();
        return detail;
    }

    public async Task<WorkOrderState?> GetStateAsync(Guid workOrderId, CancellationToken cancellationToken)
    {
        var row = await session.QuerySingleOrDefaultAsync<StateRow>(
            """
            SELECT w.work_order_id, w.status, w.assigned_mechanic_id,
                (SELECT count(*) FROM work_order_items i WHERE i.work_order_id = w.work_order_id)::int AS line_item_count
            FROM work_orders w
            WHERE w.work_order_id = @WorkOrderId
            """,
            new { WorkOrderId = workOrderId },
            cancellationToken);

        return row is null ? null : new WorkOrderState(row.WorkOrderId, row.Status, row.AssignedMechanicId, row.LineItemCount);
    }

    public async Task CreateAsync(NewWorkOrder workOrder, CancellationToken cancellationToken) =>
        await session.ExecuteAsync(
            """
            INSERT INTO work_orders (work_order_id, customer_id, vehicle_id, description, status, mileage_km, created_by_user_id, created_at, updated_at)
            VALUES (@WorkOrderId, @CustomerId, @VehicleId, @Description, 'Received', @MileageKm, @CreatedByUserId, @CreatedAtUtc, @CreatedAtUtc);

            INSERT INTO work_order_history (history_id, work_order_id, from_status, to_status, changed_by_user_id, changed_at, correlation_id, note)
            VALUES (@HistoryId, @WorkOrderId, NULL, 'Received', @CreatedByUserId, @CreatedAtUtc, @CorrelationId, 'Work order created.');
            """,
            new
            {
                workOrder.WorkOrderId,
                workOrder.CustomerId,
                workOrder.VehicleId,
                workOrder.Description,
                workOrder.MileageKm,
                workOrder.CreatedByUserId,
                workOrder.CreatedAtUtc,
                workOrder.CorrelationId,
                HistoryId = Guid.NewGuid()
            },
            cancellationToken);

    public async Task<bool> TryTransitionAsync(Guid workOrderId, WorkOrderStatus expectedStatus, WorkOrderStatus newStatus, Guid changedByUserId, string correlationId, string? note, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var updated = await session.ExecuteAsync(
            """
            UPDATE work_orders
            SET status = @NewStatus,
                updated_at = @NowUtc,
                completed_at = CASE WHEN @NewStatus = 'Completed' THEN @NowUtc ELSE completed_at END
            WHERE work_order_id = @WorkOrderId AND status = @ExpectedStatus
            """,
            new { WorkOrderId = workOrderId, ExpectedStatus = expectedStatus.ToString(), NewStatus = newStatus.ToString(), NowUtc = nowUtc },
            cancellationToken);

        if (updated == 0)
        {
            return false;
        }

        await InsertHistoryAsync(workOrderId, expectedStatus, newStatus, changedByUserId, correlationId, note, nowUtc, cancellationToken);
        return true;
    }

    public async Task<bool> TryAssignMechanicAsync(Guid workOrderId, WorkOrderStatus expectedStatus, Guid mechanicId, Guid changedByUserId, string correlationId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var updated = await session.ExecuteAsync(
            "UPDATE work_orders SET assigned_mechanic_id = @MechanicId, updated_at = @NowUtc WHERE work_order_id = @WorkOrderId AND status = @ExpectedStatus",
            new { WorkOrderId = workOrderId, MechanicId = mechanicId, ExpectedStatus = expectedStatus.ToString(), NowUtc = nowUtc },
            cancellationToken);

        if (updated == 0)
        {
            return false;
        }

        await session.ExecuteAsync(
            """
            INSERT INTO work_order_history (history_id, work_order_id, from_status, to_status, changed_by_user_id, changed_at, correlation_id, note)
            SELECT @HistoryId, @WorkOrderId, @Status, @Status, @ChangedByUserId, @NowUtc, @CorrelationId, 'Mechanic assigned: ' || full_name
            FROM users
            WHERE user_id = @MechanicId
            """,
            new { HistoryId = Guid.NewGuid(), WorkOrderId = workOrderId, Status = expectedStatus.ToString(), ChangedByUserId = changedByUserId, NowUtc = nowUtc, CorrelationId = correlationId, MechanicId = mechanicId },
            cancellationToken);

        return true;
    }

    public async Task<bool> TryAddLineItemAsync(WorkOrderLineItem item, WorkOrderStatus expectedStatus, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var updated = await session.ExecuteAsync(
            "UPDATE work_orders SET total_amount = total_amount + @LineTotal, updated_at = @NowUtc WHERE work_order_id = @WorkOrderId AND status = @ExpectedStatus",
            new { item.WorkOrderId, item.LineTotal, ExpectedStatus = expectedStatus.ToString(), NowUtc = nowUtc },
            cancellationToken);

        if (updated == 0)
        {
            return false;
        }

        await session.ExecuteAsync(
            """
            INSERT INTO work_order_items (line_item_id, work_order_id, item_type, description, quantity, unit_price, line_total, added_by_user_id, created_at)
            VALUES (@LineItemId, @WorkOrderId, @ItemType, @Description, @Quantity, @UnitPrice, @LineTotal, @AddedByUserId, @CreatedAtUtc)
            """,
            new { item.LineItemId, item.WorkOrderId, ItemType = item.ItemType.ToString(), item.Description, item.Quantity, item.UnitPrice, item.LineTotal, item.AddedByUserId, item.CreatedAtUtc },
            cancellationToken);

        return true;
    }

    private Task InsertHistoryAsync(Guid workOrderId, WorkOrderStatus fromStatus, WorkOrderStatus toStatus, Guid changedByUserId, string correlationId, string? note, DateTime nowUtc, CancellationToken cancellationToken) =>
        session.ExecuteAsync(
            """
            INSERT INTO work_order_history (history_id, work_order_id, from_status, to_status, changed_by_user_id, changed_at, correlation_id, note)
            VALUES (@HistoryId, @WorkOrderId, @FromStatus, @ToStatus, @ChangedByUserId, @NowUtc, @CorrelationId, @Note)
            """,
            new { HistoryId = Guid.NewGuid(), WorkOrderId = workOrderId, FromStatus = fromStatus.ToString(), ToStatus = toStatus.ToString(), ChangedByUserId = changedByUserId, NowUtc = nowUtc, CorrelationId = correlationId, Note = note },
            cancellationToken);

    private sealed class StateRow
    {
        public Guid WorkOrderId { get; init; }

        public WorkOrderStatus Status { get; init; }

        public Guid? AssignedMechanicId { get; init; }

        public int LineItemCount { get; init; }
    }
}

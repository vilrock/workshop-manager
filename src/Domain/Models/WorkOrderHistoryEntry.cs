using Domain.Enums;

namespace Domain.Models;

public sealed class WorkOrderHistoryEntry
{
    public Guid HistoryId { get; init; }

    public Guid WorkOrderId { get; init; }

    public WorkOrderStatus? FromStatus { get; init; }

    public WorkOrderStatus ToStatus { get; init; }

    public Guid ChangedByUserId { get; init; }

    public string ChangedByName { get; init; } = string.Empty;

    public DateTime ChangedAtUtc { get; init; }

    public string CorrelationId { get; init; } = string.Empty;

    public string? Note { get; init; }
}

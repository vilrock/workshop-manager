namespace Domain.Models;

public sealed class WorkOrderDetail : WorkOrderSummary
{
    public int MileageKm { get; init; }

    public string CreatedByName { get; init; } = string.Empty;

    public DateTime? CompletedAtUtc { get; init; }

    public IReadOnlyList<WorkOrderLineItem> Items { get; set; } = [];

    public IReadOnlyList<WorkOrderHistoryEntry> History { get; set; } = [];
}

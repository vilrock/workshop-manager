using Domain.Enums;

namespace Domain.Models;

public class WorkOrderSummary
{
    public Guid WorkOrderId { get; init; }

    public int OrderNumber { get; init; }

    public WorkOrderStatus Status { get; init; }

    public Guid CustomerId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public Guid VehicleId { get; init; }

    public string VehicleDescription { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Guid? AssignedMechanicId { get; init; }

    public string? AssignedMechanicName { get; init; }

    public decimal TotalAmount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

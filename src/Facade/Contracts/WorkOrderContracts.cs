using Domain.Enums;

namespace Facade.Contracts;

public sealed record ListWorkOrdersRequest(WorkOrderStatus? Status, Guid? MechanicId, string? Search, int Page = 1, int PageSize = 20);

public sealed record CreateWorkOrderRequest(Guid CustomerId, Guid VehicleId, string Description, int MileageKm);

public sealed record AssignMechanicRequest(Guid MechanicId);

public sealed record TransitionWorkOrderRequest(WorkOrderStatus ToStatus, string? Note, WorkOrderStatus? ExpectedStatus = null);

public sealed record AddLineItemRequest(LineItemType ItemType, string Description, decimal Quantity, decimal UnitPrice);

public sealed record LineItemResponse(Guid LineItemId, LineItemType ItemType, string Description, decimal Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record HistoryEntryResponse(
    Guid HistoryId,
    WorkOrderStatus? FromStatus,
    WorkOrderStatus ToStatus,
    Guid ChangedByUserId,
    string ChangedByName,
    DateTime ChangedAtUtc,
    string CorrelationId,
    string? Note);

public sealed record WorkOrderSummaryResponse(
    Guid WorkOrderId,
    string OrderNumber,
    WorkOrderStatus Status,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehicleDescription,
    string Description,
    Guid? AssignedMechanicId,
    string? AssignedMechanicName,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<WorkOrderStatus> AllowedTransitions);

public sealed record WorkOrderDetailResponse(
    Guid WorkOrderId,
    string OrderNumber,
    WorkOrderStatus Status,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehicleDescription,
    string Description,
    Guid? AssignedMechanicId,
    string? AssignedMechanicName,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<WorkOrderStatus> AllowedTransitions,
    int MileageKm,
    string CreatedByName,
    DateTime? CompletedAtUtc,
    IReadOnlyList<LineItemResponse> Items,
    IReadOnlyList<HistoryEntryResponse> History,
    bool CanAssignMechanic,
    bool CanAddItems);

namespace Domain.Models;

public sealed record NewWorkOrder(
    Guid WorkOrderId,
    Guid CustomerId,
    Guid VehicleId,
    string Description,
    int MileageKm,
    Guid CreatedByUserId,
    string CorrelationId,
    DateTime CreatedAtUtc);

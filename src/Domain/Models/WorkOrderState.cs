using Domain.Enums;

namespace Domain.Models;

public sealed record WorkOrderState(Guid WorkOrderId, WorkOrderStatus Status, Guid? AssignedMechanicId, int LineItemCount);

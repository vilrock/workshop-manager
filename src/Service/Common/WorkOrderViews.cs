using Domain.Abstractions;
using Domain.Enums;
using Domain.Models;
using Domain.WorkOrders;

namespace Service.Common;

public sealed record WorkOrderSummaryView(WorkOrderSummary Summary, IReadOnlyList<WorkOrderStatus> AllowedTransitions);

public sealed record WorkOrderDetailView(WorkOrderDetail Detail, IReadOnlyList<WorkOrderStatus> AllowedTransitions, bool CanAssignMechanic, bool CanAddItems);

public static class WorkOrderViewFactory
{
    public static WorkOrderSummaryView ToSummaryView(WorkOrderSummary summary, ICurrentUser user) =>
        new(summary, WorkOrderStateMachine.GetAllowedTransitions(summary.Status, user.Role, user.UserId, summary.AssignedMechanicId));

    public static WorkOrderDetailView ToDetailView(WorkOrderDetail detail, ICurrentUser user) =>
        new(
            detail,
            WorkOrderStateMachine.GetAllowedTransitions(detail.Status, user.Role, user.UserId, detail.AssignedMechanicId),
            user.Role != UserRole.Mechanic && WorkOrderStateMachine.CanAssignMechanic(detail.Status),
            WorkOrderStateMachine.CanAccess(user.Role, user.UserId, detail.AssignedMechanicId) && WorkOrderStateMachine.CanEditItems(detail.Status));
}

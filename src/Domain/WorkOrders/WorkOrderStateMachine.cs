using Domain.Common;
using Domain.Enums;
using Domain.Models;

namespace Domain.WorkOrders;

public static class WorkOrderStateMachine
{
    private static readonly IReadOnlySet<UserRole> FloorStaff = new HashSet<UserRole> { UserRole.Admin, UserRole.Mechanic };

    private static readonly IReadOnlySet<UserRole> FrontDesk = new HashSet<UserRole> { UserRole.Admin, UserRole.Advisor };

    private static readonly IReadOnlyList<TransitionRule> Rules =
    [
        new(WorkOrderStatus.Received, WorkOrderStatus.Diagnosed, FloorStaff),
        new(WorkOrderStatus.Received, WorkOrderStatus.Cancelled, FrontDesk),
        new(WorkOrderStatus.Diagnosed, WorkOrderStatus.Approved, FrontDesk),
        new(WorkOrderStatus.Diagnosed, WorkOrderStatus.Cancelled, FrontDesk),
        new(WorkOrderStatus.Approved, WorkOrderStatus.InProgress, FloorStaff),
        new(WorkOrderStatus.Approved, WorkOrderStatus.Cancelled, FrontDesk),
        new(WorkOrderStatus.InProgress, WorkOrderStatus.Completed, FloorStaff),
        new(WorkOrderStatus.Completed, WorkOrderStatus.Delivered, FrontDesk)
    ];

    private static readonly IReadOnlySet<WorkOrderStatus> StatusesRequiringMechanic = new HashSet<WorkOrderStatus>
    {
        WorkOrderStatus.Diagnosed,
        WorkOrderStatus.InProgress,
        WorkOrderStatus.Completed
    };

    private static readonly IReadOnlySet<WorkOrderStatus> ItemEditableStatuses = new HashSet<WorkOrderStatus>
    {
        WorkOrderStatus.Received,
        WorkOrderStatus.Diagnosed,
        WorkOrderStatus.Approved,
        WorkOrderStatus.InProgress
    };

    private static readonly IReadOnlySet<WorkOrderStatus> AssignableStatuses = ItemEditableStatuses;

    public static bool CanAccess(UserRole role, Guid userId, Guid? assignedMechanicId) =>
        role != UserRole.Mechanic || assignedMechanicId == userId;

    public static bool CanEditItems(WorkOrderStatus status) => ItemEditableStatuses.Contains(status);

    public static bool CanAssignMechanic(WorkOrderStatus status) => AssignableStatuses.Contains(status);

    public static IReadOnlyList<WorkOrderStatus> GetAllowedTransitions(WorkOrderStatus status, UserRole role, Guid userId, Guid? assignedMechanicId)
    {
        if (!CanAccess(role, userId, assignedMechanicId))
        {
            return [];
        }

        return Rules
            .Where(rule => rule.From == status && rule.Roles.Contains(role))
            .Select(rule => rule.To)
            .ToList();
    }

    public static Result Evaluate(WorkOrderState state, WorkOrderStatus target, UserRole role, Guid userId)
    {
        if (!CanAccess(role, userId, state.AssignedMechanicId))
        {
            return Result.Failure(DomainErrors.WorkOrders.NotAssignedToUser);
        }

        var rule = Rules.FirstOrDefault(candidate => candidate.From == state.Status && candidate.To == target);
        if (rule is null)
        {
            return Result.Failure(DomainErrors.WorkOrders.TransitionNotAllowed(state.Status, target));
        }

        if (!rule.Roles.Contains(role))
        {
            return Result.Failure(DomainErrors.WorkOrders.TransitionForbidden(role.ToString(), state.Status, target));
        }

        return CheckPreconditions(state, target);
    }

    private static Result CheckPreconditions(WorkOrderState state, WorkOrderStatus target)
    {
        if (StatusesRequiringMechanic.Contains(target) && state.AssignedMechanicId is null)
        {
            return Result.Failure(DomainErrors.WorkOrders.MechanicRequired);
        }

        if (target == WorkOrderStatus.Approved && state.LineItemCount == 0)
        {
            return Result.Failure(DomainErrors.WorkOrders.ItemsRequired);
        }

        return Result.Success();
    }
}

using Domain.Common;
using Domain.Enums;
using Domain.WorkOrders;
using UnitTests.Support;

namespace UnitTests.WorkOrderRules;

public sealed class WorkOrderStateMachineTests
{
    private static readonly Guid MechanicId = Guid.NewGuid();

    [Theory]
    [InlineData(WorkOrderStatus.Received, WorkOrderStatus.Diagnosed, UserRole.Mechanic, true)]
    [InlineData(WorkOrderStatus.Received, WorkOrderStatus.Diagnosed, UserRole.Admin, true)]
    [InlineData(WorkOrderStatus.Received, WorkOrderStatus.Diagnosed, UserRole.Advisor, false)]
    [InlineData(WorkOrderStatus.Diagnosed, WorkOrderStatus.Approved, UserRole.Advisor, true)]
    [InlineData(WorkOrderStatus.Diagnosed, WorkOrderStatus.Approved, UserRole.Admin, true)]
    [InlineData(WorkOrderStatus.Diagnosed, WorkOrderStatus.Approved, UserRole.Mechanic, false)]
    [InlineData(WorkOrderStatus.Approved, WorkOrderStatus.InProgress, UserRole.Mechanic, true)]
    [InlineData(WorkOrderStatus.Approved, WorkOrderStatus.InProgress, UserRole.Advisor, false)]
    [InlineData(WorkOrderStatus.InProgress, WorkOrderStatus.Completed, UserRole.Mechanic, true)]
    [InlineData(WorkOrderStatus.InProgress, WorkOrderStatus.Completed, UserRole.Advisor, false)]
    [InlineData(WorkOrderStatus.Completed, WorkOrderStatus.Delivered, UserRole.Advisor, true)]
    [InlineData(WorkOrderStatus.Completed, WorkOrderStatus.Delivered, UserRole.Mechanic, false)]
    [InlineData(WorkOrderStatus.Received, WorkOrderStatus.Cancelled, UserRole.Advisor, true)]
    [InlineData(WorkOrderStatus.Approved, WorkOrderStatus.Cancelled, UserRole.Advisor, true)]
    [InlineData(WorkOrderStatus.Approved, WorkOrderStatus.Cancelled, UserRole.Mechanic, false)]
    public void Evaluate_applies_the_role_required_by_each_transition(WorkOrderStatus from, WorkOrderStatus to, UserRole role, bool allowed)
    {
        var userId = role == UserRole.Mechanic ? MechanicId : Guid.NewGuid();
        var state = Builders.State(from, MechanicId, lineItemCount: 2);

        var result = WorkOrderStateMachine.Evaluate(state, to, role, userId);

        Assert.Equal(allowed, result.IsSuccess);
        if (!allowed)
        {
            Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        }
    }

    [Theory]
    [InlineData(WorkOrderStatus.Received, WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Received, WorkOrderStatus.Delivered)]
    [InlineData(WorkOrderStatus.Diagnosed, WorkOrderStatus.InProgress)]
    [InlineData(WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled)]
    [InlineData(WorkOrderStatus.Completed, WorkOrderStatus.Cancelled)]
    [InlineData(WorkOrderStatus.Delivered, WorkOrderStatus.Received)]
    [InlineData(WorkOrderStatus.Cancelled, WorkOrderStatus.Received)]
    [InlineData(WorkOrderStatus.Approved, WorkOrderStatus.Diagnosed)]
    public void Evaluate_rejects_transitions_that_do_not_exist(WorkOrderStatus from, WorkOrderStatus to)
    {
        var state = Builders.State(from, MechanicId);

        var result = WorkOrderStateMachine.Evaluate(state, to, UserRole.Admin, Guid.NewGuid());

        Assert.Equal(ErrorType.BusinessRule, result.Error!.Type);
        Assert.Equal("work_orders.invalid_transition", result.Error.Code);
    }

    [Fact]
    public void Evaluate_hides_unassigned_orders_from_mechanics()
    {
        var state = Builders.State(WorkOrderStatus.Received, MechanicId);

        var result = WorkOrderStateMachine.Evaluate(state, WorkOrderStatus.Diagnosed, UserRole.Mechanic, Guid.NewGuid());

        Assert.Equal(DomainErrors.WorkOrders.NotAssignedToUser, result.Error);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Diagnosed)]
    [InlineData(WorkOrderStatus.InProgress)]
    [InlineData(WorkOrderStatus.Completed)]
    public void Evaluate_requires_an_assigned_mechanic_for_floor_work(WorkOrderStatus target)
    {
        var from = target switch
        {
            WorkOrderStatus.Diagnosed => WorkOrderStatus.Received,
            WorkOrderStatus.InProgress => WorkOrderStatus.Approved,
            _ => WorkOrderStatus.InProgress
        };
        var state = Builders.State(from, mechanicId: null);

        var result = WorkOrderStateMachine.Evaluate(state, target, UserRole.Admin, Guid.NewGuid());

        Assert.Equal(DomainErrors.WorkOrders.MechanicRequired, result.Error);
    }

    [Fact]
    public void Evaluate_requires_at_least_one_line_item_to_approve()
    {
        var state = Builders.State(WorkOrderStatus.Diagnosed, MechanicId, lineItemCount: 0);

        var result = WorkOrderStateMachine.Evaluate(state, WorkOrderStatus.Approved, UserRole.Advisor, Guid.NewGuid());

        Assert.Equal(DomainErrors.WorkOrders.ItemsRequired, result.Error);
    }

    [Fact]
    public void GetAllowedTransitions_for_a_mechanic_only_covers_assigned_orders()
    {
        var own = WorkOrderStateMachine.GetAllowedTransitions(WorkOrderStatus.InProgress, UserRole.Mechanic, MechanicId, MechanicId);
        var foreign = WorkOrderStateMachine.GetAllowedTransitions(WorkOrderStatus.InProgress, UserRole.Mechanic, Guid.NewGuid(), MechanicId);

        Assert.Equal([WorkOrderStatus.Completed], own);
        Assert.Empty(foreign);
    }

    [Fact]
    public void GetAllowedTransitions_offers_cancel_to_the_front_desk_before_work_starts()
    {
        var allowed = WorkOrderStateMachine.GetAllowedTransitions(WorkOrderStatus.Diagnosed, UserRole.Advisor, Guid.NewGuid(), MechanicId);

        Assert.Equivalent(new[] { WorkOrderStatus.Approved, WorkOrderStatus.Cancelled }, allowed);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Delivered)]
    [InlineData(WorkOrderStatus.Cancelled)]
    public void Terminal_statuses_have_no_transitions(WorkOrderStatus status)
    {
        foreach (var role in Enum.GetValues<UserRole>())
        {
            Assert.Empty(WorkOrderStateMachine.GetAllowedTransitions(status, role, MechanicId, MechanicId));
        }
    }

    [Theory]
    [InlineData(WorkOrderStatus.Received, true)]
    [InlineData(WorkOrderStatus.InProgress, true)]
    [InlineData(WorkOrderStatus.Completed, false)]
    [InlineData(WorkOrderStatus.Delivered, false)]
    [InlineData(WorkOrderStatus.Cancelled, false)]
    public void Line_items_are_only_editable_before_completion(WorkOrderStatus status, bool editable)
    {
        Assert.Equal(editable, WorkOrderStateMachine.CanEditItems(status));
    }
}

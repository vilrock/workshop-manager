using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using NSubstitute;
using Service.Commands.TransitionWorkOrder;
using UnitTests.Support;

namespace UnitTests.Handlers;

public sealed class TransitionWorkOrderHandlerTests
{
    private readonly IWorkOrderRepository workOrders = Substitute.For<IWorkOrderRepository>();

    private WorkOrderState ArrangeOrder(WorkOrderStatus status, Guid? mechanicId, int lineItemCount = 1, bool transitionSucceeds = true)
    {
        var state = Builders.State(status, mechanicId, lineItemCount);
        workOrders.GetStateAsync(state.WorkOrderId, Arg.Any<CancellationToken>()).Returns(state);
        workOrders.GetDetailAsync(state.WorkOrderId, Arg.Any<CancellationToken>()).Returns(Builders.DetailFor(state));
        workOrders.TryTransitionAsync(state.WorkOrderId, Arg.Any<WorkOrderStatus>(), Arg.Any<WorkOrderStatus>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(transitionSucceeds);
        return state;
    }

    private Task<Result<Service.Common.WorkOrderDetailView>> Send(TestCurrentUser user, WorkOrderState state, WorkOrderStatus target, string? note = null, WorkOrderStatus? expected = null) =>
        new TransitionWorkOrderHandler(user, FixedClock.Default, workOrders)
            .Handle(new TransitionWorkOrderCommand(state.WorkOrderId, target, note, expected), CancellationToken.None);

    [Fact]
    public async Task Assigned_mechanic_can_diagnose_and_the_audit_user_comes_from_the_token()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var state = ArrangeOrder(WorkOrderStatus.Received, mechanic.UserId);

        var result = await Send(mechanic, state, WorkOrderStatus.Diagnosed, "  Brake pads worn  ");

        Assert.True(result.IsSuccess);
        await workOrders.Received(1).TryTransitionAsync(
            state.WorkOrderId,
            WorkOrderStatus.Received,
            WorkOrderStatus.Diagnosed,
            mechanic.UserId,
            mechanic.CorrelationId,
            "Brake pads worn",
            FixedClock.Default.UtcNow,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Advisor_approves_a_diagnosed_order_that_has_line_items()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var state = ArrangeOrder(WorkOrderStatus.Diagnosed, Guid.NewGuid(), lineItemCount: 3);

        var result = await Send(advisor, state, WorkOrderStatus.Approved);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Mechanic_cannot_touch_an_order_assigned_to_someone_else()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var state = ArrangeOrder(WorkOrderStatus.Received, Guid.NewGuid());

        var result = await Send(mechanic, state, WorkOrderStatus.Diagnosed);

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        await workOrders.DidNotReceiveWithAnyArgs().TryTransitionAsync(default, default, default, default, default!, default, default, default);
    }

    [Fact]
    public async Task Advisor_cannot_perform_a_mechanic_transition()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var state = ArrangeOrder(WorkOrderStatus.Received, Guid.NewGuid());

        var result = await Send(advisor, state, WorkOrderStatus.Diagnosed);

        Assert.Equal("work_orders.transition_forbidden", result.Error!.Code);
    }

    [Fact]
    public async Task Mechanic_cannot_approve_even_on_their_own_order()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var state = ArrangeOrder(WorkOrderStatus.Diagnosed, mechanic.UserId);

        var result = await Send(mechanic, state, WorkOrderStatus.Approved);

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }

    [Fact]
    public async Task Skipping_a_step_is_a_business_rule_violation()
    {
        var admin = TestCurrentUser.As(UserRole.Admin);
        var state = ArrangeOrder(WorkOrderStatus.Received, Guid.NewGuid());

        var result = await Send(admin, state, WorkOrderStatus.Completed);

        Assert.Equal(ErrorType.BusinessRule, result.Error!.Type);
    }

    [Fact]
    public async Task Approval_without_line_items_is_rejected()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var state = ArrangeOrder(WorkOrderStatus.Diagnosed, Guid.NewGuid(), lineItemCount: 0);

        var result = await Send(advisor, state, WorkOrderStatus.Approved);

        Assert.Equal(DomainErrors.WorkOrders.ItemsRequired, result.Error);
    }

    [Fact]
    public async Task Losing_the_atomic_update_race_returns_a_conflict()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var state = ArrangeOrder(WorkOrderStatus.Received, mechanic.UserId, transitionSucceeds: false);

        var result = await Send(mechanic, state, WorkOrderStatus.Diagnosed);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        await workOrders.DidNotReceiveWithAnyArgs().GetDetailAsync(default, default);
    }

    [Fact]
    public async Task A_stale_expected_status_returns_a_conflict_without_updating()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var state = ArrangeOrder(WorkOrderStatus.Diagnosed, mechanic.UserId);

        var result = await Send(mechanic, state, WorkOrderStatus.Approved, expected: WorkOrderStatus.Received);

        Assert.Equal(DomainErrors.WorkOrders.ConcurrentModification, result.Error);
        await workOrders.DidNotReceiveWithAnyArgs().TryTransitionAsync(default, default, default, default, default!, default, default, default);
    }

    [Fact]
    public async Task Unknown_order_returns_not_found()
    {
        var admin = TestCurrentUser.As(UserRole.Admin);
        var missing = Builders.State(WorkOrderStatus.Received);
        workOrders.GetStateAsync(missing.WorkOrderId, Arg.Any<CancellationToken>()).Returns((WorkOrderState?)null);

        var result = await Send(admin, missing, WorkOrderStatus.Diagnosed);

        Assert.Equal(DomainErrors.WorkOrders.NotFound, result.Error);
    }
}

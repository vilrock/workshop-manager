using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using NSubstitute;
using Service.Queries.GetWorkOrder;
using Service.Queries.ListWorkOrders;
using UnitTests.Support;

namespace UnitTests.Handlers;

public sealed class WorkOrderQueryHandlerTests
{
    private readonly IWorkOrderRepository workOrders = Substitute.For<IWorkOrderRepository>();

    [Fact]
    public async Task Mechanic_cannot_read_an_order_assigned_to_someone_else()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var state = Builders.State(WorkOrderStatus.Received, Guid.NewGuid());
        workOrders.GetDetailAsync(state.WorkOrderId, Arg.Any<CancellationToken>()).Returns(Builders.DetailFor(state));

        var result = await new GetWorkOrderHandler(mechanic, workOrders).Handle(new GetWorkOrderQuery(state.WorkOrderId), CancellationToken.None);

        Assert.Equal(DomainErrors.WorkOrders.NotAssignedToUser, result.Error);
    }

    [Fact]
    public async Task Assigned_mechanic_reads_the_order_with_the_actions_they_can_run()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var state = Builders.State(WorkOrderStatus.Approved, mechanic.UserId);
        workOrders.GetDetailAsync(state.WorkOrderId, Arg.Any<CancellationToken>()).Returns(Builders.DetailFor(state));

        var result = await new GetWorkOrderHandler(mechanic, workOrders).Handle(new GetWorkOrderQuery(state.WorkOrderId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([WorkOrderStatus.InProgress], result.Value.AllowedTransitions);
        Assert.True(result.Value.CanAddItems);
        Assert.False(result.Value.CanAssignMechanic);
    }

    [Fact]
    public async Task Advisor_reads_any_order_and_may_assign_mechanics()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var state = Builders.State(WorkOrderStatus.Received, Guid.NewGuid());
        workOrders.GetDetailAsync(state.WorkOrderId, Arg.Any<CancellationToken>()).Returns(Builders.DetailFor(state));

        var result = await new GetWorkOrderHandler(advisor, workOrders).Handle(new GetWorkOrderQuery(state.WorkOrderId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.CanAssignMechanic);
        Assert.Equal([WorkOrderStatus.Cancelled], result.Value.AllowedTransitions);
    }

    [Fact]
    public async Task Missing_order_returns_not_found()
    {
        workOrders.GetDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WorkOrderDetail?)null);

        var result = await new GetWorkOrderHandler(TestCurrentUser.As(UserRole.Admin), workOrders).Handle(new GetWorkOrderQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(DomainErrors.WorkOrders.NotFound, result.Error);
    }

    [Fact]
    public async Task Mechanic_listing_is_always_scoped_to_their_own_orders()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var someoneElse = Guid.NewGuid();
        workOrders.ListAsync(Arg.Any<WorkOrderFilter>(), Arg.Any<CancellationToken>()).Returns(new PagedResult<WorkOrderSummary>([], 1, 20, 0));

        await new ListWorkOrdersHandler(mechanic, workOrders).Handle(new ListWorkOrdersQuery(null, someoneElse, null, 1, 20), CancellationToken.None);

        await workOrders.Received(1).ListAsync(Arg.Is<WorkOrderFilter>(filter => filter.MechanicId == mechanic.UserId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Advisor_listing_honours_the_requested_mechanic_filter()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var mechanicId = Guid.NewGuid();
        workOrders.ListAsync(Arg.Any<WorkOrderFilter>(), Arg.Any<CancellationToken>()).Returns(new PagedResult<WorkOrderSummary>([], 1, 20, 0));

        await new ListWorkOrdersHandler(advisor, workOrders).Handle(new ListWorkOrdersQuery(WorkOrderStatus.InProgress, mechanicId, " brake ", 1, 20), CancellationToken.None);

        await workOrders.Received(1).ListAsync(
            Arg.Is<WorkOrderFilter>(filter => filter.MechanicId == mechanicId && filter.Status == WorkOrderStatus.InProgress && filter.Search == "brake"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Listing_decorates_each_order_with_the_transitions_available_to_the_caller()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var summary = new WorkOrderSummary { WorkOrderId = Guid.NewGuid(), Status = WorkOrderStatus.InProgress, AssignedMechanicId = mechanic.UserId };
        workOrders.ListAsync(Arg.Any<WorkOrderFilter>(), Arg.Any<CancellationToken>()).Returns(new PagedResult<WorkOrderSummary>([summary], 1, 20, 1));

        var result = await new ListWorkOrdersHandler(mechanic, workOrders).Handle(new ListWorkOrdersQuery(null, null, null, 1, 20), CancellationToken.None);

        Assert.Equal([WorkOrderStatus.Completed], result.Value.Items.Single().AllowedTransitions);
        Assert.Equal(1, result.Value.TotalCount);
    }
}

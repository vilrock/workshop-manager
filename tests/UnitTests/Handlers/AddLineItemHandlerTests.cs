using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using NSubstitute;
using Service.Commands.AddLineItem;
using UnitTests.Support;

namespace UnitTests.Handlers;

public sealed class AddLineItemHandlerTests
{
    private readonly IWorkOrderRepository workOrders = Substitute.For<IWorkOrderRepository>();

    private WorkOrderState ArrangeOrder(WorkOrderStatus status, Guid? mechanicId, bool insertSucceeds = true)
    {
        var state = Builders.State(status, mechanicId);
        workOrders.GetStateAsync(state.WorkOrderId, Arg.Any<CancellationToken>()).Returns(state);
        workOrders.GetDetailAsync(state.WorkOrderId, Arg.Any<CancellationToken>()).Returns(Builders.DetailFor(state));
        workOrders.TryAddLineItemAsync(Arg.Any<WorkOrderLineItem>(), Arg.Any<WorkOrderStatus>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(insertSucceeds);
        return state;
    }

    private Task<Result<Service.Common.WorkOrderDetailView>> Send(TestCurrentUser user, WorkOrderState state, decimal quantity = 2.5m, decimal unitPrice = 12.35m) =>
        new AddLineItemHandler(user, FixedClock.Default, workOrders)
            .Handle(new AddLineItemCommand(state.WorkOrderId, LineItemType.Part, "  Oil filter ", quantity, unitPrice), CancellationToken.None);

    [Fact]
    public async Task The_line_total_is_calculated_on_the_server_and_audited_with_the_token_user()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var state = ArrangeOrder(WorkOrderStatus.InProgress, mechanic.UserId);

        var result = await Send(mechanic, state);

        Assert.True(result.IsSuccess);
        await workOrders.Received(1).TryAddLineItemAsync(
            Arg.Is<WorkOrderLineItem>(item =>
                item.LineTotal == 30.88m &&
                item.Description == "Oil filter" &&
                item.AddedByUserId == mechanic.UserId &&
                item.WorkOrderId == state.WorkOrderId),
            WorkOrderStatus.InProgress,
            FixedClock.Default.UtcNow,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Advisor_can_add_lines_to_any_open_order()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var state = ArrangeOrder(WorkOrderStatus.Diagnosed, Guid.NewGuid());

        var result = await Send(advisor, state);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Mechanic_cannot_add_lines_to_someone_elses_order()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var state = ArrangeOrder(WorkOrderStatus.InProgress, Guid.NewGuid());

        var result = await Send(mechanic, state);

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        await workOrders.DidNotReceiveWithAnyArgs().TryAddLineItemAsync(default!, default, default, default);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Delivered)]
    [InlineData(WorkOrderStatus.Cancelled)]
    public async Task Lines_are_locked_once_the_order_is_completed_or_closed(WorkOrderStatus status)
    {
        var admin = TestCurrentUser.As(UserRole.Admin);
        var state = ArrangeOrder(status, Guid.NewGuid());

        var result = await Send(admin, state);

        Assert.Equal(DomainErrors.WorkOrders.ItemsLocked, result.Error);
    }

    [Fact]
    public async Task A_status_change_during_the_insert_returns_a_conflict()
    {
        var admin = TestCurrentUser.As(UserRole.Admin);
        var state = ArrangeOrder(WorkOrderStatus.InProgress, Guid.NewGuid(), insertSucceeds: false);

        var result = await Send(admin, state);

        Assert.Equal(DomainErrors.WorkOrders.ConcurrentModification, result.Error);
    }

    [Fact]
    public async Task Unknown_order_returns_not_found()
    {
        var admin = TestCurrentUser.As(UserRole.Admin);
        var missing = Builders.State(WorkOrderStatus.Received);
        workOrders.GetStateAsync(missing.WorkOrderId, Arg.Any<CancellationToken>()).Returns((WorkOrderState?)null);

        var result = await Send(admin, missing);

        Assert.Equal(DomainErrors.WorkOrders.NotFound, result.Error);
    }
}

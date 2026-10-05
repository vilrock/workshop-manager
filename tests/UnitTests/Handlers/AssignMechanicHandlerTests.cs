using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using NSubstitute;
using Service.Commands.AssignMechanic;
using UnitTests.Support;

namespace UnitTests.Handlers;

public sealed class AssignMechanicHandlerTests
{
    private readonly IUserRepository users = Substitute.For<IUserRepository>();
    private readonly IWorkOrderRepository workOrders = Substitute.For<IWorkOrderRepository>();

    private WorkOrderState ArrangeOrder(WorkOrderStatus status, bool assignSucceeds = true)
    {
        var state = Builders.State(status);
        workOrders.GetStateAsync(state.WorkOrderId, Arg.Any<CancellationToken>()).Returns(state);
        workOrders.GetDetailAsync(state.WorkOrderId, Arg.Any<CancellationToken>()).Returns(Builders.DetailFor(state));
        workOrders.TryAssignMechanicAsync(state.WorkOrderId, Arg.Any<WorkOrderStatus>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(assignSucceeds);
        return state;
    }

    private Task<Result<Service.Common.WorkOrderDetailView>> Send(TestCurrentUser user, WorkOrderState state, User? target)
    {
        var mechanicId = target?.UserId ?? Guid.NewGuid();
        users.GetByIdAsync(mechanicId, Arg.Any<CancellationToken>()).Returns(target);
        return new AssignMechanicHandler(user, FixedClock.Default, users, workOrders).Handle(new AssignMechanicCommand(state.WorkOrderId, mechanicId), CancellationToken.None);
    }

    [Fact]
    public async Task Advisor_assigns_an_active_mechanic_and_the_change_is_audited_with_the_token_user()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var state = ArrangeOrder(WorkOrderStatus.Received);
        var mechanic = Builders.UserWithRole(UserRole.Mechanic);

        var result = await Send(advisor, state, mechanic);

        Assert.True(result.IsSuccess);
        await workOrders.Received(1).TryAssignMechanicAsync(state.WorkOrderId, WorkOrderStatus.Received, mechanic.UserId, advisor.UserId, advisor.CorrelationId, FixedClock.Default.UtcNow, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mechanics_cannot_assign_work()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var state = ArrangeOrder(WorkOrderStatus.Received);

        var result = await Send(mechanic, state, Builders.UserWithRole(UserRole.Mechanic));

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }

    [Theory]
    [InlineData(UserRole.Advisor, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.Mechanic, false)]
    public async Task Only_active_mechanics_can_be_assigned(UserRole targetRole, bool targetActive)
    {
        var admin = TestCurrentUser.As(UserRole.Admin);
        var state = ArrangeOrder(WorkOrderStatus.Received);

        var result = await Send(admin, state, Builders.UserWithRole(targetRole, targetActive));

        Assert.Equal(DomainErrors.WorkOrders.MechanicNotFound, result.Error);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Delivered)]
    [InlineData(WorkOrderStatus.Cancelled)]
    public async Task Assignment_is_locked_after_completion(WorkOrderStatus status)
    {
        var admin = TestCurrentUser.As(UserRole.Admin);
        var state = ArrangeOrder(status);

        var result = await Send(admin, state, Builders.UserWithRole(UserRole.Mechanic));

        Assert.Equal(DomainErrors.WorkOrders.AssignmentLocked, result.Error);
    }

    [Fact]
    public async Task A_status_change_during_assignment_returns_a_conflict()
    {
        var admin = TestCurrentUser.As(UserRole.Admin);
        var state = ArrangeOrder(WorkOrderStatus.Received, assignSucceeds: false);

        var result = await Send(admin, state, Builders.UserWithRole(UserRole.Mechanic));

        Assert.Equal(DomainErrors.WorkOrders.ConcurrentModification, result.Error);
    }
}

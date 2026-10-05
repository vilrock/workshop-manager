using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using NSubstitute;
using Service.Commands.CreateWorkOrder;
using UnitTests.Support;

namespace UnitTests.Handlers;

public sealed class CreateWorkOrderHandlerTests
{
    private readonly ICustomerRepository customers = Substitute.For<ICustomerRepository>();
    private readonly IVehicleRepository vehicles = Substitute.For<IVehicleRepository>();
    private readonly IWorkOrderRepository workOrders = Substitute.For<IWorkOrderRepository>();
    private readonly IIdempotencyRepository idempotency = Substitute.For<IIdempotencyRepository>();
    private readonly Customer customer = Builders.Customer();

    private CreateWorkOrderHandler CreateHandler(TestCurrentUser user) => new(user, FixedClock.Default, customers, vehicles, workOrders, idempotency);

    private CreateWorkOrderCommand Command(Vehicle vehicle, string key = "key-1", string description = "Check brakes") =>
        new(key, customer.CustomerId, vehicle.VehicleId, description, 45000);

    private Vehicle ArrangeVehicle(Guid? ownerId = null)
    {
        var vehicle = Builders.VehicleOf(ownerId ?? customer.CustomerId);
        customers.GetAsync(customer.CustomerId, Arg.Any<CancellationToken>()).Returns(customer);
        vehicles.GetAsync(vehicle.VehicleId, Arg.Any<CancellationToken>()).Returns(vehicle);
        workOrders.GetDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(call => Builders.DetailFor(new WorkOrderState(call.Arg<Guid>(), WorkOrderStatus.Received, null, 0)));
        return vehicle;
    }

    [Fact]
    public async Task A_new_key_creates_the_order_audited_with_the_token_user_and_correlation_id()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var vehicle = ArrangeVehicle();
        idempotency.TryRegisterAsync("key-1", advisor.UserId, Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler(advisor).Handle(Command(vehicle, description: "  Check brakes "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsReplay);
        await workOrders.Received(1).CreateAsync(
            Arg.Is<NewWorkOrder>(order =>
                order.CreatedByUserId == advisor.UserId &&
                order.CorrelationId == advisor.CorrelationId &&
                order.Description == "Check brakes" &&
                order.CustomerId == customer.CustomerId),
            Arg.Any<CancellationToken>());
        await idempotency.Received(1).AttachWorkOrderAsync("key-1", advisor.UserId, Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replaying_the_same_key_and_payload_returns_the_original_order_without_creating_another()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var vehicle = ArrangeVehicle();
        var originalId = Guid.NewGuid();
        string? storedHash = null;
        idempotency.TryRegisterAsync("key-1", advisor.UserId, Arg.Do<string>(hash => storedHash = hash), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true, false);
        idempotency.GetAsync("key-1", advisor.UserId, Arg.Any<CancellationToken>()).Returns(_ => new IdempotencyRecord(storedHash!, originalId));
        var handler = CreateHandler(advisor);

        await handler.Handle(Command(vehicle), CancellationToken.None);
        workOrders.ClearReceivedCalls();
        var replay = await handler.Handle(Command(vehicle), CancellationToken.None);

        Assert.True(replay.IsSuccess);
        Assert.True(replay.Value.IsReplay);
        Assert.Equal(originalId, replay.Value.View.Detail.WorkOrderId);
        await workOrders.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task Reusing_a_key_with_a_different_payload_is_rejected()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var vehicle = ArrangeVehicle();
        idempotency.TryRegisterAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(false);
        idempotency.GetAsync("key-1", advisor.UserId, Arg.Any<CancellationToken>()).Returns(new IdempotencyRecord("another-hash", Guid.NewGuid()));

        var result = await CreateHandler(advisor).Handle(Command(vehicle), CancellationToken.None);

        Assert.Equal(DomainErrors.WorkOrders.IdempotencyKeyReused, result.Error);
    }

    [Fact]
    public async Task A_vehicle_from_another_customer_is_rejected()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var vehicle = ArrangeVehicle(ownerId: Guid.NewGuid());
        idempotency.TryRegisterAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler(advisor).Handle(Command(vehicle), CancellationToken.None);

        Assert.Equal(DomainErrors.Vehicles.DoesNotBelongToCustomer, result.Error);
        await workOrders.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task Orders_cannot_be_opened_for_inactive_customers()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        var inactive = Builders.Customer(isActive: false);
        var vehicle = Builders.VehicleOf(inactive.CustomerId);
        customers.GetAsync(inactive.CustomerId, Arg.Any<CancellationToken>()).Returns(inactive);
        idempotency.TryRegisterAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler(advisor).Handle(new CreateWorkOrderCommand("key-1", inactive.CustomerId, vehicle.VehicleId, "Check", 1), CancellationToken.None);

        Assert.Equal("customers.inactive", result.Error!.Code);
    }

    [Fact]
    public async Task Unknown_customer_returns_not_found()
    {
        var advisor = TestCurrentUser.As(UserRole.Advisor);
        customers.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);
        idempotency.TryRegisterAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler(advisor).Handle(new CreateWorkOrderCommand("key-1", Guid.NewGuid(), Guid.NewGuid(), "Check", 1), CancellationToken.None);

        Assert.Equal(DomainErrors.Customers.NotFound, result.Error);
    }

    [Fact]
    public async Task Mechanics_cannot_create_orders()
    {
        var mechanic = TestCurrentUser.As(UserRole.Mechanic);
        var vehicle = ArrangeVehicle();

        var result = await CreateHandler(mechanic).Handle(Command(vehicle), CancellationToken.None);

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        await idempotency.DidNotReceiveWithAnyArgs().TryRegisterAsync(default!, default, default!, default, default);
    }
}

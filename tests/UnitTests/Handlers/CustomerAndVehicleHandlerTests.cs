using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using NSubstitute;
using Service.Commands.CreateCustomer;
using Service.Commands.CreateVehicle;
using Service.Commands.UpdateCustomer;
using Service.Commands.UpdateVehicle;
using UnitTests.Support;

namespace UnitTests.Handlers;

public sealed class CustomerAndVehicleHandlerTests
{
    private readonly ICustomerRepository customers = Substitute.For<ICustomerRepository>();
    private readonly IVehicleRepository vehicles = Substitute.For<IVehicleRepository>();

    [Fact]
    public async Task Creating_a_customer_normalizes_the_email_and_activates_it()
    {
        var handler = new CreateCustomerHandler(customers, FixedClock.Default);

        var result = await handler.Handle(new CreateCustomerCommand("  Ava Thompson ", "  Ava.Thompson@Example.COM ", " +1 555 0105 ", "  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ava.thompson@example.com", result.Value.Email);
        Assert.Equal("Ava Thompson", result.Value.FullName);
        Assert.Null(result.Value.Address);
        Assert.True(result.Value.IsActive);
        await customers.Received(1).CreateAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creating_a_customer_with_a_used_email_returns_a_conflict()
    {
        customers.EmailExistsAsync("ava@example.com", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new CreateCustomerHandler(customers, FixedClock.Default).Handle(new CreateCustomerCommand("Ava", "AVA@example.com", "+1 555 0105", null), CancellationToken.None);

        Assert.Equal(DomainErrors.Customers.EmailAlreadyExists, result.Error);
        await customers.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task Updating_a_customer_excludes_itself_from_the_email_uniqueness_check()
    {
        var existing = Builders.Customer();
        customers.GetAsync(existing.CustomerId, Arg.Any<CancellationToken>()).Returns(existing);
        customers.UpdateAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await new UpdateCustomerHandler(customers, FixedClock.Default).Handle(new UpdateCustomerCommand(existing.CustomerId, "Olivia M.", existing.Email, existing.Phone, null, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        await customers.Received(1).EmailExistsAsync(existing.Email, existing.CustomerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Updating_an_unknown_customer_returns_not_found()
    {
        customers.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await new UpdateCustomerHandler(customers, FixedClock.Default).Handle(new UpdateCustomerCommand(Guid.NewGuid(), "A", "a@example.com", "+1 555 0100", null, true), CancellationToken.None);

        Assert.Equal(DomainErrors.Customers.NotFound, result.Error);
    }

    [Fact]
    public async Task Creating_a_vehicle_uppercases_plate_and_vin()
    {
        var customer = Builders.Customer();
        customers.GetAsync(customer.CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await new CreateVehicleHandler(customers, vehicles).Handle(new CreateVehicleCommand(customer.CustomerId, " kdp-4821 ", "Toyota", "Corolla", 2019, "2t1burhe0kc123401", " ", 1000), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("KDP-4821", result.Value.LicensePlate);
        Assert.Equal("2T1BURHE0KC123401", result.Value.Vin);
        Assert.Null(result.Value.Color);
        Assert.Equal(customer.FullName, result.Value.CustomerName);
    }

    [Fact]
    public async Task Creating_a_vehicle_with_a_used_plate_returns_a_conflict()
    {
        var customer = Builders.Customer();
        customers.GetAsync(customer.CustomerId, Arg.Any<CancellationToken>()).Returns(customer);
        vehicles.LicensePlateExistsAsync("KDP-4821", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new CreateVehicleHandler(customers, vehicles).Handle(new CreateVehicleCommand(customer.CustomerId, "kdp-4821", "Toyota", "Corolla", 2019, null, null, 0), CancellationToken.None);

        Assert.Equal(DomainErrors.Vehicles.LicensePlateAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Creating_a_vehicle_for_an_unknown_customer_returns_not_found()
    {
        customers.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await new CreateVehicleHandler(customers, vehicles).Handle(new CreateVehicleCommand(Guid.NewGuid(), "AAA-111", "Ford", "Fiesta", 2015, null, null, 0), CancellationToken.None);

        Assert.Equal(DomainErrors.Customers.NotFound, result.Error);
    }

    [Fact]
    public async Task Updating_a_vehicle_keeps_the_owner_and_excludes_itself_from_the_plate_check()
    {
        var existing = Builders.VehicleOf(Guid.NewGuid());
        vehicles.GetAsync(existing.VehicleId, Arg.Any<CancellationToken>()).Returns(existing);
        vehicles.UpdateAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await new UpdateVehicleHandler(vehicles).Handle(new UpdateVehicleCommand(existing.VehicleId, existing.LicensePlate, "Toyota", "Corolla", 2020, null, "Blue", 70000), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.CustomerId, result.Value.CustomerId);
        await vehicles.Received(1).LicensePlateExistsAsync(existing.LicensePlate, existing.VehicleId, Arg.Any<CancellationToken>());
    }
}

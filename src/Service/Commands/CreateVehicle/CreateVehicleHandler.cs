using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using MediatR;
using Service.Common;

namespace Service.Commands.CreateVehicle;

public sealed class CreateVehicleHandler(ICustomerRepository customers, IVehicleRepository vehicles) : IRequestHandler<CreateVehicleCommand, Result<Vehicle>>
{
    public async Task<Result<Vehicle>> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        var customer = await customers.GetAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return DomainErrors.Customers.NotFound;
        }

        var licensePlate = TextNormalization.NormalizeLicensePlate(request.LicensePlate);
        if (await vehicles.LicensePlateExistsAsync(licensePlate, null, cancellationToken))
        {
            return DomainErrors.Vehicles.LicensePlateAlreadyExists;
        }

        var vehicle = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            CustomerId = customer.CustomerId,
            CustomerName = customer.FullName,
            LicensePlate = licensePlate,
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            Vin = TextNormalization.NullIfBlank(request.Vin)?.ToUpperInvariant(),
            Color = TextNormalization.NullIfBlank(request.Color),
            MileageKm = request.MileageKm
        };

        await vehicles.CreateAsync(vehicle, cancellationToken);
        return vehicle;
    }
}

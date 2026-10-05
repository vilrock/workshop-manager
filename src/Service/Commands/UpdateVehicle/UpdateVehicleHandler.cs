using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using MediatR;
using Service.Common;

namespace Service.Commands.UpdateVehicle;

public sealed class UpdateVehicleHandler(IVehicleRepository vehicles) : IRequestHandler<UpdateVehicleCommand, Result<Vehicle>>
{
    public async Task<Result<Vehicle>> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        var existing = await vehicles.GetAsync(request.VehicleId, cancellationToken);
        if (existing is null)
        {
            return DomainErrors.Vehicles.NotFound;
        }

        var licensePlate = TextNormalization.NormalizeLicensePlate(request.LicensePlate);
        if (await vehicles.LicensePlateExistsAsync(licensePlate, request.VehicleId, cancellationToken))
        {
            return DomainErrors.Vehicles.LicensePlateAlreadyExists;
        }

        var updated = new Vehicle
        {
            VehicleId = existing.VehicleId,
            CustomerId = existing.CustomerId,
            CustomerName = existing.CustomerName,
            LicensePlate = licensePlate,
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            Vin = TextNormalization.NullIfBlank(request.Vin)?.ToUpperInvariant(),
            Color = TextNormalization.NullIfBlank(request.Color),
            MileageKm = request.MileageKm
        };

        return await vehicles.UpdateAsync(updated, cancellationToken) ? updated : DomainErrors.Vehicles.NotFound;
    }
}

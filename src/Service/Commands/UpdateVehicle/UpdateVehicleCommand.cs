using Domain.Abstractions;
using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Commands.UpdateVehicle;

public sealed record UpdateVehicleCommand(Guid VehicleId, string LicensePlate, string Make, string Model, int Year, string? Vin, string? Color, int MileageKm) : IRequest<Result<Vehicle>>, ITransactionalCommand;

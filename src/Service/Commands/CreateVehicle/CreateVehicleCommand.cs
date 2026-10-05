using Domain.Abstractions;
using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Commands.CreateVehicle;

public sealed record CreateVehicleCommand(Guid CustomerId, string LicensePlate, string Make, string Model, int Year, string? Vin, string? Color, int MileageKm) : IRequest<Result<Vehicle>>, ITransactionalCommand;

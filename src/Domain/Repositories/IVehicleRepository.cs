using Domain.Common;
using Domain.Models;

namespace Domain.Repositories;

public interface IVehicleRepository
{
    Task<IReadOnlyList<Vehicle>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    Task<PagedResult<Vehicle>> ListAsync(string? search, Guid? customerId, int page, int pageSize, CancellationToken cancellationToken);

    Task<Vehicle?> GetAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<bool> LicensePlateExistsAsync(string licensePlate, Guid? excludedVehicleId, CancellationToken cancellationToken);

    Task CreateAsync(Vehicle vehicle, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken);
}

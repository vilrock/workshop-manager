using Dapper;
using Domain.Common;
using Domain.Models;
using Domain.Repositories;

namespace Data.Postgres.Repositories;

public sealed class VehicleRepository(PostgresSession session) : IVehicleRepository
{
    private const string SelectColumns = """
        SELECT v.vehicle_id, v.customer_id, c.full_name AS customer_name, v.license_plate, v.make, v.model, v.year, v.vin, v.color, v.mileage_km
        FROM vehicles v
        JOIN customers c ON c.customer_id = v.customer_id
        """;

    private const string ListFilter = """
        (@CustomerId::uuid IS NULL OR v.customer_id = @CustomerId)
        AND (@Pattern::text IS NULL
            OR v.license_plate ILIKE @Pattern ESCAPE '!'
            OR v.make ILIKE @Pattern ESCAPE '!'
            OR v.model ILIKE @Pattern ESCAPE '!'
            OR c.full_name ILIKE @Pattern ESCAPE '!')
        """;

    public Task<IReadOnlyList<Vehicle>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        session.QueryAsync<Vehicle>($"{SelectColumns} WHERE v.customer_id = @CustomerId ORDER BY v.make, v.model, v.vehicle_id", new { CustomerId = customerId }, cancellationToken);

    public async Task<PagedResult<Vehicle>> ListAsync(string? search, Guid? customerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var parameters = new { Pattern = LikePattern.FromSearch(search), CustomerId = customerId, PageSize = pageSize, Offset = (page - 1) * pageSize };
        var sql = $"""
            SELECT count(*)::int FROM vehicles v JOIN customers c ON c.customer_id = v.customer_id WHERE {ListFilter};
            {SelectColumns} WHERE {ListFilter} ORDER BY v.license_plate, v.vehicle_id LIMIT @PageSize OFFSET @Offset;
            """;

        using var grid = await session.QueryMultipleAsync(sql, parameters, cancellationToken);
        var totalCount = await grid.ReadSingleAsync<int>();
        var items = (await grid.ReadAsync<Vehicle>()).AsList();
        return new PagedResult<Vehicle>(items, page, pageSize, totalCount);
    }

    public Task<Vehicle?> GetAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        session.QuerySingleOrDefaultAsync<Vehicle>($"{SelectColumns} WHERE v.vehicle_id = @VehicleId", new { VehicleId = vehicleId }, cancellationToken);

    public Task<bool> LicensePlateExistsAsync(string licensePlate, Guid? excludedVehicleId, CancellationToken cancellationToken) =>
        session.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM vehicles WHERE upper(license_plate) = @LicensePlate AND (@ExcludedVehicleId::uuid IS NULL OR vehicle_id <> @ExcludedVehicleId))",
            new { LicensePlate = licensePlate, ExcludedVehicleId = excludedVehicleId },
            cancellationToken);

    public async Task CreateAsync(Vehicle vehicle, CancellationToken cancellationToken) =>
        await session.ExecuteAsync(
            """
            INSERT INTO vehicles (vehicle_id, customer_id, license_plate, make, model, year, vin, color, mileage_km)
            VALUES (@VehicleId, @CustomerId, @LicensePlate, @Make, @Model, @Year, @Vin, @Color, @MileageKm)
            """,
            vehicle,
            cancellationToken);

    public async Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken) =>
        await session.ExecuteAsync(
            """
            UPDATE vehicles
            SET license_plate = @LicensePlate, make = @Make, model = @Model, year = @Year, vin = @Vin, color = @Color, mileage_km = @MileageKm
            WHERE vehicle_id = @VehicleId
            """,
            vehicle,
            cancellationToken) == 1;
}

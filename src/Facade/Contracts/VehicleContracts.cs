namespace Facade.Contracts;

public sealed record ListVehiclesRequest(string? Search, Guid? CustomerId, int Page = 1, int PageSize = 10);

public sealed record VehicleRequest(string LicensePlate, string Make, string Model, int Year, string? Vin, string? Color, int MileageKm);

public sealed record VehicleResponse(
    Guid VehicleId,
    Guid CustomerId,
    string CustomerName,
    string LicensePlate,
    string Make,
    string Model,
    int Year,
    string? Vin,
    string? Color,
    int MileageKm);

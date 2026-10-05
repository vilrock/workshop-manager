namespace Domain.Models;

public sealed class Vehicle
{
    public Guid VehicleId { get; init; }

    public Guid CustomerId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public string LicensePlate { get; init; } = string.Empty;

    public string Make { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public int Year { get; init; }

    public string? Vin { get; init; }

    public string? Color { get; init; }

    public int MileageKm { get; init; }
}

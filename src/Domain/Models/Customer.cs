namespace Domain.Models;

public sealed class Customer
{
    public Guid CustomerId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string? Address { get; init; }

    public bool IsActive { get; init; }

    public int VehicleCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

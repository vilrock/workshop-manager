using Domain.Abstractions;
using Domain.Enums;
using Domain.Models;

namespace UnitTests.Support;

internal sealed class TestCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; init; } = true;

    public Guid UserId { get; init; } = Guid.NewGuid();

    public string Email { get; init; } = "user@example.com";

    public UserRole Role { get; init; }

    public string CorrelationId { get; init; } = "test-correlation-id";

    public static TestCurrentUser As(UserRole role, Guid? userId = null) =>
        new() { Role = role, UserId = userId ?? Guid.NewGuid() };
}

internal sealed class FixedClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; } = utcNow;

    public static FixedClock Default { get; } = new(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));
}

internal static class Builders
{
    public static WorkOrderState State(WorkOrderStatus status, Guid? mechanicId = null, int lineItemCount = 1) =>
        new(Guid.NewGuid(), status, mechanicId, lineItemCount);

    public static WorkOrderDetail DetailFor(WorkOrderState state) =>
        new()
        {
            WorkOrderId = state.WorkOrderId,
            OrderNumber = 1001,
            Status = state.Status,
            AssignedMechanicId = state.AssignedMechanicId,
            CustomerName = "Olivia Martin",
            VehicleDescription = "2019 Toyota Corolla (KDP-4821)",
            Description = "Test order",
            CreatedByName = "Sofia Bennett"
        };

    public static User UserWithRole(UserRole role, bool isActive = true) =>
        new()
        {
            UserId = Guid.NewGuid(),
            Email = $"{role.ToString().ToLowerInvariant()}@example.com",
            FullName = $"Test {role}",
            Role = role,
            IsActive = isActive,
            PasswordHash = "hash"
        };

    public static Customer Customer(bool isActive = true) =>
        new() { CustomerId = Guid.NewGuid(), FullName = "Olivia Martin", Email = "olivia@example.com", Phone = "+1 555 0101", IsActive = isActive };

    public static Vehicle VehicleOf(Guid customerId) =>
        new() { VehicleId = Guid.NewGuid(), CustomerId = customerId, CustomerName = "Olivia Martin", LicensePlate = "KDP-4821", Make = "Toyota", Model = "Corolla", Year = 2019 };
}

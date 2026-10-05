namespace Facade.Contracts;

public sealed record ListCustomersRequest(string? Search, int Page = 1, int PageSize = 10);

public sealed record CreateCustomerRequest(string FullName, string Email, string Phone, string? Address);

public sealed record UpdateCustomerRequest(string FullName, string Email, string Phone, string? Address, bool IsActive);

public sealed record CustomerResponse(
    Guid CustomerId,
    string FullName,
    string Email,
    string Phone,
    string? Address,
    bool IsActive,
    int VehicleCount,
    DateTime CreatedAtUtc);

using Domain.Common;
using Domain.Models;

namespace Domain.Repositories;

public interface ICustomerRepository
{
    Task<PagedResult<Customer>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    Task<Customer?> GetAsync(Guid customerId, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, Guid? excludedCustomerId, CancellationToken cancellationToken);

    Task CreateAsync(Customer customer, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Customer customer, CancellationToken cancellationToken);
}

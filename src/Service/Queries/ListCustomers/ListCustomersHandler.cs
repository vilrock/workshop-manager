using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using MediatR;

namespace Service.Queries.ListCustomers;

public sealed class ListCustomersHandler(ICustomerRepository customers) : IRequestHandler<ListCustomersQuery, Result<PagedResult<Customer>>>
{
    public async Task<Result<PagedResult<Customer>>> Handle(ListCustomersQuery request, CancellationToken cancellationToken) =>
        Result<PagedResult<Customer>>.Success(await customers.ListAsync(request.Search?.Trim(), request.Page, request.PageSize, cancellationToken));
}

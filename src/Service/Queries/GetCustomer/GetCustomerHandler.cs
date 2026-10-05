using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using MediatR;

namespace Service.Queries.GetCustomer;

public sealed class GetCustomerHandler(ICustomerRepository customers) : IRequestHandler<GetCustomerQuery, Result<Customer>>
{
    public async Task<Result<Customer>> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        var customer = await customers.GetAsync(request.CustomerId, cancellationToken);
        return customer is null ? DomainErrors.Customers.NotFound : customer;
    }
}

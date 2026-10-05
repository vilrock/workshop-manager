using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using MediatR;

namespace Service.Queries.ListCustomerVehicles;

public sealed class ListCustomerVehiclesHandler(ICustomerRepository customers, IVehicleRepository vehicles) : IRequestHandler<ListCustomerVehiclesQuery, Result<IReadOnlyList<Vehicle>>>
{
    public async Task<Result<IReadOnlyList<Vehicle>>> Handle(ListCustomerVehiclesQuery request, CancellationToken cancellationToken)
    {
        if (await customers.GetAsync(request.CustomerId, cancellationToken) is null)
        {
            return DomainErrors.Customers.NotFound;
        }

        return Result<IReadOnlyList<Vehicle>>.Success(await vehicles.ListByCustomerAsync(request.CustomerId, cancellationToken));
    }
}

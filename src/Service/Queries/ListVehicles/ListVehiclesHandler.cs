using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using MediatR;

namespace Service.Queries.ListVehicles;

public sealed class ListVehiclesHandler(IVehicleRepository vehicles) : IRequestHandler<ListVehiclesQuery, Result<PagedResult<Vehicle>>>
{
    public async Task<Result<PagedResult<Vehicle>>> Handle(ListVehiclesQuery request, CancellationToken cancellationToken) =>
        Result<PagedResult<Vehicle>>.Success(await vehicles.ListAsync(request.Search?.Trim(), request.CustomerId, request.Page, request.PageSize, cancellationToken));
}

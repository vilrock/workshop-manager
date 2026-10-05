using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Queries.ListVehicles;

public sealed record ListVehiclesQuery(string? Search, Guid? CustomerId, int Page, int PageSize) : IRequest<Result<PagedResult<Vehicle>>>;

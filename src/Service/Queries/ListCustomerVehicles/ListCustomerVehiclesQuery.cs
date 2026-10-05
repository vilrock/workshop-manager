using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Queries.ListCustomerVehicles;

public sealed record ListCustomerVehiclesQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<Vehicle>>>;

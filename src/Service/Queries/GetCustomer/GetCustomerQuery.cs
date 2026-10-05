using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Queries.GetCustomer;

public sealed record GetCustomerQuery(Guid CustomerId) : IRequest<Result<Customer>>;

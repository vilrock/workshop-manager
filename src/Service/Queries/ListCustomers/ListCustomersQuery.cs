using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Queries.ListCustomers;

public sealed record ListCustomersQuery(string? Search, int Page, int PageSize) : IRequest<Result<PagedResult<Customer>>>;

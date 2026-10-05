using Domain.Common;
using Domain.Enums;
using MediatR;
using Service.Common;

namespace Service.Queries.ListWorkOrders;

public sealed record ListWorkOrdersQuery(WorkOrderStatus? Status, Guid? MechanicId, string? Search, int Page, int PageSize) : IRequest<Result<PagedResult<WorkOrderSummaryView>>>;

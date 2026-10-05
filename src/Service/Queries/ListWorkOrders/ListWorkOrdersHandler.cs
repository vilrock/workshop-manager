using Domain.Abstractions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using MediatR;
using Service.Common;

namespace Service.Queries.ListWorkOrders;

public sealed class ListWorkOrdersHandler(ICurrentUser currentUser, IWorkOrderRepository workOrders) : IRequestHandler<ListWorkOrdersQuery, Result<PagedResult<WorkOrderSummaryView>>>
{
    public async Task<Result<PagedResult<WorkOrderSummaryView>>> Handle(ListWorkOrdersQuery request, CancellationToken cancellationToken)
    {
        var mechanicScope = currentUser.Role == UserRole.Mechanic ? currentUser.UserId : request.MechanicId;
        var filter = new WorkOrderFilter(request.Status, mechanicScope, request.Search?.Trim(), request.Page, request.PageSize);

        var page = await workOrders.ListAsync(filter, cancellationToken);
        var views = page.Items.Select(summary => WorkOrderViewFactory.ToSummaryView(summary, currentUser)).ToList();

        return new PagedResult<WorkOrderSummaryView>(views, page.Page, page.PageSize, page.TotalCount);
    }
}

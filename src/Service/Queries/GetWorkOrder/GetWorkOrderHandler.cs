using Domain.Abstractions;
using Domain.Common;
using Domain.Repositories;
using Domain.WorkOrders;
using MediatR;
using Service.Common;

namespace Service.Queries.GetWorkOrder;

public sealed class GetWorkOrderHandler(ICurrentUser currentUser, IWorkOrderRepository workOrders) : IRequestHandler<GetWorkOrderQuery, Result<WorkOrderDetailView>>
{
    public async Task<Result<WorkOrderDetailView>> Handle(GetWorkOrderQuery request, CancellationToken cancellationToken)
    {
        var detail = await workOrders.GetDetailAsync(request.WorkOrderId, cancellationToken);
        if (detail is null)
        {
            return DomainErrors.WorkOrders.NotFound;
        }

        if (!WorkOrderStateMachine.CanAccess(currentUser.Role, currentUser.UserId, detail.AssignedMechanicId))
        {
            return DomainErrors.WorkOrders.NotAssignedToUser;
        }

        return WorkOrderViewFactory.ToDetailView(detail, currentUser);
    }
}

using Domain.Abstractions;
using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.WorkOrders;
using MediatR;
using Service.Common;

namespace Service.Commands.AddLineItem;

public sealed class AddLineItemHandler(ICurrentUser currentUser, IClock clock, IWorkOrderRepository workOrders) : IRequestHandler<AddLineItemCommand, Result<WorkOrderDetailView>>
{
    public async Task<Result<WorkOrderDetailView>> Handle(AddLineItemCommand request, CancellationToken cancellationToken)
    {
        var state = await workOrders.GetStateAsync(request.WorkOrderId, cancellationToken);
        if (state is null)
        {
            return DomainErrors.WorkOrders.NotFound;
        }

        if (!WorkOrderStateMachine.CanAccess(currentUser.Role, currentUser.UserId, state.AssignedMechanicId))
        {
            return DomainErrors.WorkOrders.NotAssignedToUser;
        }

        if (!WorkOrderStateMachine.CanEditItems(state.Status))
        {
            return DomainErrors.WorkOrders.ItemsLocked;
        }

        var now = clock.UtcNow;
        var item = WorkOrderLineItem.Create(request.WorkOrderId, request.ItemType, request.Description, request.Quantity, request.UnitPrice, currentUser.UserId, now);

        if (!await workOrders.TryAddLineItemAsync(item, state.Status, now, cancellationToken))
        {
            return DomainErrors.WorkOrders.ConcurrentModification;
        }

        var detail = await workOrders.GetDetailAsync(request.WorkOrderId, cancellationToken);
        return detail is null ? DomainErrors.WorkOrders.NotFound : WorkOrderViewFactory.ToDetailView(detail, currentUser);
    }
}

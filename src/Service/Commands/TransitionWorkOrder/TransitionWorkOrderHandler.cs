using Domain.Abstractions;
using Domain.Common;
using Domain.Repositories;
using Domain.WorkOrders;
using MediatR;
using Service.Common;

namespace Service.Commands.TransitionWorkOrder;

public sealed class TransitionWorkOrderHandler(ICurrentUser currentUser, IClock clock, IWorkOrderRepository workOrders) : IRequestHandler<TransitionWorkOrderCommand, Result<WorkOrderDetailView>>
{
    public async Task<Result<WorkOrderDetailView>> Handle(TransitionWorkOrderCommand request, CancellationToken cancellationToken)
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

        if (request.ExpectedStatus is not null && request.ExpectedStatus != state.Status)
        {
            return DomainErrors.WorkOrders.ConcurrentModification;
        }

        var evaluation = WorkOrderStateMachine.Evaluate(state, request.ToStatus, currentUser.Role, currentUser.UserId);
        if (!evaluation.IsSuccess)
        {
            return evaluation.Error!;
        }

        var note = TextNormalization.NullIfBlank(request.Note);
        var transitioned = await workOrders.TryTransitionAsync(request.WorkOrderId, state.Status, request.ToStatus, currentUser.UserId, currentUser.CorrelationId, note, clock.UtcNow, cancellationToken);
        if (!transitioned)
        {
            return DomainErrors.WorkOrders.ConcurrentModification;
        }

        var detail = await workOrders.GetDetailAsync(request.WorkOrderId, cancellationToken);
        return detail is null ? DomainErrors.WorkOrders.NotFound : WorkOrderViewFactory.ToDetailView(detail, currentUser);
    }
}

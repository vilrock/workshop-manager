using Domain.Abstractions;
using Domain.Common;
using Domain.Enums;
using Domain.Repositories;
using Domain.WorkOrders;
using MediatR;
using Service.Common;

namespace Service.Commands.AssignMechanic;

public sealed class AssignMechanicHandler(ICurrentUser currentUser, IClock clock, IUserRepository users, IWorkOrderRepository workOrders) : IRequestHandler<AssignMechanicCommand, Result<WorkOrderDetailView>>
{
    public async Task<Result<WorkOrderDetailView>> Handle(AssignMechanicCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role == UserRole.Mechanic)
        {
            return Error.Forbidden("work_orders.assign_forbidden", "Mechanics cannot assign work orders.");
        }

        var state = await workOrders.GetStateAsync(request.WorkOrderId, cancellationToken);
        if (state is null)
        {
            return DomainErrors.WorkOrders.NotFound;
        }

        if (!WorkOrderStateMachine.CanAssignMechanic(state.Status))
        {
            return DomainErrors.WorkOrders.AssignmentLocked;
        }

        var mechanic = await users.GetByIdAsync(request.MechanicId, cancellationToken);
        if (mechanic is not { IsActive: true, Role: UserRole.Mechanic })
        {
            return DomainErrors.WorkOrders.MechanicNotFound;
        }

        var assigned = await workOrders.TryAssignMechanicAsync(request.WorkOrderId, state.Status, mechanic.UserId, currentUser.UserId, currentUser.CorrelationId, clock.UtcNow, cancellationToken);
        if (!assigned)
        {
            return DomainErrors.WorkOrders.ConcurrentModification;
        }

        var detail = await workOrders.GetDetailAsync(request.WorkOrderId, cancellationToken);
        return detail is null ? DomainErrors.WorkOrders.NotFound : WorkOrderViewFactory.ToDetailView(detail, currentUser);
    }
}

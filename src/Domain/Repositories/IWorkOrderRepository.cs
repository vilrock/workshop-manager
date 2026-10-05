using Domain.Common;
using Domain.Enums;
using Domain.Models;

namespace Domain.Repositories;

public interface IWorkOrderRepository
{
    Task<PagedResult<WorkOrderSummary>> ListAsync(WorkOrderFilter filter, CancellationToken cancellationToken);

    Task<WorkOrderDetail?> GetDetailAsync(Guid workOrderId, CancellationToken cancellationToken);

    Task<WorkOrderState?> GetStateAsync(Guid workOrderId, CancellationToken cancellationToken);

    Task CreateAsync(NewWorkOrder workOrder, CancellationToken cancellationToken);

    Task<bool> TryTransitionAsync(Guid workOrderId, WorkOrderStatus expectedStatus, WorkOrderStatus newStatus, Guid changedByUserId, string correlationId, string? note, DateTime nowUtc, CancellationToken cancellationToken);

    Task<bool> TryAssignMechanicAsync(Guid workOrderId, WorkOrderStatus expectedStatus, Guid mechanicId, Guid changedByUserId, string correlationId, DateTime nowUtc, CancellationToken cancellationToken);

    Task<bool> TryAddLineItemAsync(WorkOrderLineItem item, WorkOrderStatus expectedStatus, DateTime nowUtc, CancellationToken cancellationToken);
}

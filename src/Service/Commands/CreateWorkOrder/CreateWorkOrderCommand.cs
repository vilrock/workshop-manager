using Domain.Abstractions;
using Domain.Common;
using MediatR;
using Service.Common;

namespace Service.Commands.CreateWorkOrder;

public sealed record CreateWorkOrderCommand(string IdempotencyKey, Guid CustomerId, Guid VehicleId, string Description, int MileageKm) : IRequest<Result<CreateWorkOrderResult>>, ITransactionalCommand;

public sealed record CreateWorkOrderResult(WorkOrderDetailView View, bool IsReplay);

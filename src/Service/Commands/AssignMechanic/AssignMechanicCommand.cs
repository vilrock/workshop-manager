using Domain.Abstractions;
using Domain.Common;
using MediatR;
using Service.Common;

namespace Service.Commands.AssignMechanic;

public sealed record AssignMechanicCommand(Guid WorkOrderId, Guid MechanicId) : IRequest<Result<WorkOrderDetailView>>, ITransactionalCommand;

using Domain.Abstractions;
using Domain.Common;
using Domain.Enums;
using MediatR;
using Service.Common;

namespace Service.Commands.TransitionWorkOrder;

public sealed record TransitionWorkOrderCommand(Guid WorkOrderId, WorkOrderStatus ToStatus, string? Note, WorkOrderStatus? ExpectedStatus = null) : IRequest<Result<WorkOrderDetailView>>, ITransactionalCommand;

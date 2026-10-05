using Domain.Abstractions;
using Domain.Common;
using Domain.Enums;
using MediatR;
using Service.Common;

namespace Service.Commands.AddLineItem;

public sealed record AddLineItemCommand(Guid WorkOrderId, LineItemType ItemType, string Description, decimal Quantity, decimal UnitPrice) : IRequest<Result<WorkOrderDetailView>>, ITransactionalCommand;

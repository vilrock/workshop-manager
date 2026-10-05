using Domain.Common;
using MediatR;
using Service.Common;

namespace Service.Queries.GetWorkOrder;

public sealed record GetWorkOrderQuery(Guid WorkOrderId) : IRequest<Result<WorkOrderDetailView>>;

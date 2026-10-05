using Domain.Enums;

namespace Domain.WorkOrders;

public sealed record TransitionRule(WorkOrderStatus From, WorkOrderStatus To, IReadOnlySet<UserRole> Roles);

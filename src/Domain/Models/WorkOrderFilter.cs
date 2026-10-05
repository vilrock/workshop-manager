using Domain.Enums;

namespace Domain.Models;

public sealed record WorkOrderFilter(WorkOrderStatus? Status, Guid? MechanicId, string? Search, int Page, int PageSize);

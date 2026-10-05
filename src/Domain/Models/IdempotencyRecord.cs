namespace Domain.Models;

public sealed record IdempotencyRecord(string RequestHash, Guid? WorkOrderId);

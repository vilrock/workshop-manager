using Domain.WorkOrders;
using Domain.Enums;

namespace Domain.Models;

public sealed class WorkOrderLineItem
{
    public Guid LineItemId { get; init; }

    public Guid WorkOrderId { get; init; }

    public LineItemType ItemType { get; init; }

    public string Description { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }

    public Guid AddedByUserId { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public static WorkOrderLineItem Create(Guid workOrderId, LineItemType itemType, string description, decimal quantity, decimal unitPrice, Guid addedByUserId, DateTime nowUtc) =>
        new()
        {
            LineItemId = Guid.NewGuid(),
            WorkOrderId = workOrderId,
            ItemType = itemType,
            Description = description.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = WorkOrderPricing.CalculateLineTotal(quantity, unitPrice),
            AddedByUserId = addedByUserId,
            CreatedAtUtc = nowUtc
        };
}

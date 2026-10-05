using Domain.Models;

namespace Domain.WorkOrders;

public static class WorkOrderPricing
{
    public static decimal CalculateLineTotal(decimal quantity, decimal unitPrice) =>
        Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);

    public static decimal CalculateTotal(IEnumerable<WorkOrderLineItem> items) =>
        items.Sum(item => item.LineTotal);
}

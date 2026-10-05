using Domain.Enums;
using Domain.Models;
using Domain.WorkOrders;
using UnitTests.Support;

namespace UnitTests.WorkOrderRules;

public sealed class WorkOrderPricingTests
{
    [Theory]
    [InlineData(2, 12.35, 24.70)]
    [InlineData(2.5, 12.35, 30.88)]
    [InlineData(1.5, 95, 142.50)]
    [InlineData(0.01, 0.01, 0.00)]
    [InlineData(3, 0, 0)]
    public void CalculateLineTotal_rounds_to_two_decimals_away_from_zero(double quantity, double unitPrice, double expected)
    {
        var total = WorkOrderPricing.CalculateLineTotal((decimal)quantity, (decimal)unitPrice);

        Assert.Equal((decimal)expected, total);
    }

    [Fact]
    public void CalculateTotal_sums_every_line()
    {
        var now = FixedClock.Default.UtcNow;
        var workOrderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var items = new[]
        {
            WorkOrderLineItem.Create(workOrderId, LineItemType.Part, "Oil filter", 2.5m, 12.35m, userId, now),
            WorkOrderLineItem.Create(workOrderId, LineItemType.Service, "Labor", 1.5m, 95m, userId, now)
        };

        Assert.Equal(173.38m, WorkOrderPricing.CalculateTotal(items));
    }

    [Fact]
    public void Line_item_creation_trims_the_description_and_computes_the_line_total()
    {
        var item = WorkOrderLineItem.Create(Guid.NewGuid(), LineItemType.Part, "  Brake pads  ", 2, 41.5m, Guid.NewGuid(), FixedClock.Default.UtcNow);

        Assert.Equal("Brake pads", item.Description);
        Assert.Equal(83m, item.LineTotal);
    }
}

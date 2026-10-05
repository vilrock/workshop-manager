using Domain.Enums;
using FluentValidation.TestHelper;
using Service.Commands.AddLineItem;
using Service.Commands.CreateCustomer;
using Service.Commands.CreateWorkOrder;
using Service.Queries.ListWorkOrders;

namespace UnitTests.Handlers;

public sealed class ValidatorTests
{
    [Theory]
    [InlineData(0, 10, false)]
    [InlineData(-1, 10, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    [InlineData(1, 100, true)]
    [InlineData(3, 20, true)]
    public void Paging_is_bounded(int page, int pageSize, bool valid)
    {
        var result = new ListWorkOrdersValidator().TestValidate(new ListWorkOrdersQuery(null, null, null, page, pageSize));

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData(0, 10, false)]
    [InlineData(1, -1, false)]
    [InlineData(1, 12.345, false)]
    [InlineData(1001, 10, false)]
    [InlineData(2.5, 12.35, true)]
    [InlineData(1, 0, true)]
    public void Line_item_amounts_are_validated(double quantity, double unitPrice, bool valid)
    {
        var command = new AddLineItemCommand(Guid.NewGuid(), LineItemType.Part, "Filter", (decimal)quantity, (decimal)unitPrice);

        Assert.Equal(valid, new AddLineItemValidator().TestValidate(command).IsValid);
    }

    [Fact]
    public void Line_items_need_a_description()
    {
        var command = new AddLineItemCommand(Guid.NewGuid(), LineItemType.Service, " ", 1, 10);

        new AddLineItemValidator().TestValidate(command).ShouldHaveValidationErrorFor(item => item.Description);
    }

    [Fact]
    public void Work_orders_require_an_idempotency_key_and_a_description()
    {
        var command = new CreateWorkOrderCommand(string.Empty, Guid.NewGuid(), Guid.NewGuid(), "ab", 100);

        var result = new CreateWorkOrderValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.IdempotencyKey);
        result.ShouldHaveValidationErrorFor(item => item.Description);
    }

    [Theory]
    [InlineData("+1 555 0101", true)]
    [InlineData("(555) 010-1234", true)]
    [InlineData("abc", false)]
    [InlineData("123", false)]
    public void Customer_phone_numbers_are_checked(string phone, bool valid)
    {
        var result = new CreateCustomerValidator().TestValidate(new CreateCustomerCommand("Ava", "ava@example.com", phone, null));

        Assert.Equal(valid, result.IsValid);
    }
}

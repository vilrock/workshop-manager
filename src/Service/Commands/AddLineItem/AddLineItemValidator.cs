using FluentValidation;

namespace Service.Commands.AddLineItem;

public sealed class AddLineItemValidator : AbstractValidator<AddLineItemCommand>
{
    public AddLineItemValidator()
    {
        RuleFor(command => command.WorkOrderId).NotEmpty();
        RuleFor(command => command.ItemType).IsInEnum();
        RuleFor(command => command.Description).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Quantity).GreaterThan(0).LessThanOrEqualTo(1000).PrecisionScale(10, 2, false);
        RuleFor(command => command.UnitPrice).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1_000_000).PrecisionScale(12, 2, false);
    }
}

using FluentValidation;

namespace Service.Commands.TransitionWorkOrder;

public sealed class TransitionWorkOrderValidator : AbstractValidator<TransitionWorkOrderCommand>
{
    public TransitionWorkOrderValidator()
    {
        RuleFor(command => command.WorkOrderId).NotEmpty();
        RuleFor(command => command.ToStatus).IsInEnum();
        RuleFor(command => command.Note).MaximumLength(500);
    }
}

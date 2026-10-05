using FluentValidation;

namespace Service.Commands.AssignMechanic;

public sealed class AssignMechanicValidator : AbstractValidator<AssignMechanicCommand>
{
    public AssignMechanicValidator()
    {
        RuleFor(command => command.WorkOrderId).NotEmpty();
        RuleFor(command => command.MechanicId).NotEmpty();
    }
}

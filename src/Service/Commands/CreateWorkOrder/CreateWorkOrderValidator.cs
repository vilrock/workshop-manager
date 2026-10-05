using FluentValidation;

namespace Service.Commands.CreateWorkOrder;

public sealed class CreateWorkOrderValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderValidator()
    {
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.VehicleId).NotEmpty();
        RuleFor(command => command.Description).NotEmpty().MinimumLength(3).MaximumLength(500);
        RuleFor(command => command.MileageKm).InclusiveBetween(0, 2_000_000);
    }
}

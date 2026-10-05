using FluentValidation;

namespace Service.Commands.CreateVehicle;

public sealed class CreateVehicleValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.LicensePlate).NotEmpty().MaximumLength(12);
        RuleFor(command => command.Make).NotEmpty().MaximumLength(60);
        RuleFor(command => command.Model).NotEmpty().MaximumLength(60);
        RuleFor(command => command.Year).InclusiveBetween(1950, DateTime.UtcNow.Year + 1);
        RuleFor(command => command.Vin).MaximumLength(17);
        RuleFor(command => command.Color).MaximumLength(30);
        RuleFor(command => command.MileageKm).InclusiveBetween(0, 2_000_000);
    }
}

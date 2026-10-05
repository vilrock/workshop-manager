using FluentValidation;
using Service.Common;

namespace Service.Commands.CreateCustomer;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Phone).MustBeValidPhone();
        RuleFor(command => command.Address).MaximumLength(250);
    }
}

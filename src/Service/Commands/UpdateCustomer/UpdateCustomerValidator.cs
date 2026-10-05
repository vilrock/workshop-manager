using FluentValidation;
using Service.Common;

namespace Service.Commands.UpdateCustomer;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Phone).MustBeValidPhone();
        RuleFor(command => command.Address).MaximumLength(250);
    }
}

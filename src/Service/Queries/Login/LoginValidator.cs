using FluentValidation;

namespace Service.Queries.Login;

public sealed class LoginValidator : AbstractValidator<LoginQuery>
{
    public LoginValidator()
    {
        RuleFor(query => query.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(query => query.Password).NotEmpty().MaximumLength(128);
    }
}

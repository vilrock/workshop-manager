using FluentValidation;

namespace Service.Common;

public static class FieldValidationExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeValidPhone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().Matches("^[-0-9+() ]{7,25}$").WithMessage("Phone must contain 7 to 25 digits, spaces or + ( ) - characters.");
}

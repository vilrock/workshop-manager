using FluentValidation;

namespace Service.Common;

public static class PagingValidationExtensions
{
    public const int MaxPageSize = 100;

    public static IRuleBuilderOptions<T, int> MustBeValidPage<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThanOrEqualTo(1);

    public static IRuleBuilderOptions<T, int> MustBeValidPageSize<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, MaxPageSize);
}

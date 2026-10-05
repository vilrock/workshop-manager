using FluentValidation;
using Service.Common;

namespace Service.Queries.ListVehicles;

public sealed class ListVehiclesValidator : AbstractValidator<ListVehiclesQuery>
{
    public ListVehiclesValidator()
    {
        RuleFor(query => query.Page).MustBeValidPage();
        RuleFor(query => query.PageSize).MustBeValidPageSize();
        RuleFor(query => query.Search).MaximumLength(100);
    }
}

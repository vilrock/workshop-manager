using FluentValidation;
using Service.Common;

namespace Service.Queries.ListCustomers;

public sealed class ListCustomersValidator : AbstractValidator<ListCustomersQuery>
{
    public ListCustomersValidator()
    {
        RuleFor(query => query.Page).MustBeValidPage();
        RuleFor(query => query.PageSize).MustBeValidPageSize();
        RuleFor(query => query.Search).MaximumLength(100);
    }
}

using FluentValidation;
using Service.Common;

namespace Service.Queries.ListWorkOrders;

public sealed class ListWorkOrdersValidator : AbstractValidator<ListWorkOrdersQuery>
{
    public ListWorkOrdersValidator()
    {
        RuleFor(query => query.Page).MustBeValidPage();
        RuleFor(query => query.PageSize).MustBeValidPageSize();
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.Status).IsInEnum();
    }
}

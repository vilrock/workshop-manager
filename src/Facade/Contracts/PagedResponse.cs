namespace Facade.Contracts;

public sealed record PagedResponse<TItem>(IReadOnlyList<TItem> Items, int Page, int PageSize, int TotalCount, int TotalPages);

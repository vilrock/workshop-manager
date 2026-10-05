namespace Domain.Common;

public sealed record PagedResult<TItem>(IReadOnlyList<TItem> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

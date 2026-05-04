namespace SPSS.App.Common;
public static class PaginationHelper<T>
{
    public static async ValueTask<PagedResponse<T>> CreateAsync(IQueryable<T> source, PaginationFilter filter, CancellationToken cancellationToken)
    {
        var index = filter.Index;
        var size = filter.Size;
        if (filter.Index < 1)
        {
            index = 1;
        }
        if (filter.Size > 500 || filter.Size < 1)
        {
            size = 10;
        }

        var count = await source.CountAsync(cancellationToken);
        var items = await source.Skip((index - 1) * size).Take(size).ToListAsync(cancellationToken);

        return new PagedResponse<T>
        {
            Data = items,
            Index = index,
            Size = size,
            Count = count
        };
    }
}
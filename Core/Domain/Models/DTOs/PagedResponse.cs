namespace SPSS.Domain.Models.DTOs;

public sealed class PagedResponse<T>
{
    public PagedResponse()
    {
    }

    public PagedResponse(IEnumerable<T> data)
    {
        Data = data;
    }

    public IEnumerable<T>? Data { get; set; }
    public int? Index { get; set; }
    public int? Size { get; set; }
    public int? Count { get; set; }
    public bool HasPrevious => Index > 1;
    public bool HasNext => Index < Count / Size;
}
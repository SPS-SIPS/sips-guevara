namespace SPSS.Domain.Interfaces;
public interface IResponse
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; set; }
    public string Message { get; init; }
    public int CacheInMins { get; set; }
}
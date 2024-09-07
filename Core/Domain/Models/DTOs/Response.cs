

namespace SPSS.Domain.Models.DTOs;
public sealed record Response<T>(T Data) : IResponse
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; set; }
    public string Message { get; init; } = string.Empty; // Add the 'required' modifier or declare the property as nullable.
    public int CacheInMins { get; set; } = 0;
    public static Response<T> BadRequest(string message, T? result = default)
    {
        return new Response<T>(result ?? default!)
        {
            IsSuccess = false,
            Message = message,
            StatusCode = 400
        };
    }

    public static Response<T> NotFound(string message, T? result = default)
    {
        return new Response<T>(result ?? default!)
        {
            IsSuccess = false,
            Message = message,
            StatusCode = 404
        };
    }

    public static Response<T> InternalServerError(string message, T? result = default)
    {
        return new Response<T>(result ?? default!)
        {
            IsSuccess = false,
            Message = message,
            StatusCode = 500
        };
    }

    // Conflict status code 409
    public static Response<T> Conflict(string message, T? result = default)
    {
        return new Response<T>(result ?? default!)
        {
            IsSuccess = false,
            Message = message,
            StatusCode = 409
        };
    }

    public static Response<T> Success(T result, int cacheInMins)
    {
        return new Response<T>(result)
        {
            IsSuccess = true,
            Message = "Success",
            StatusCode = 200,
            CacheInMins = cacheInMins
        };
    }
}


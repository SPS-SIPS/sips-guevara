using System.Net;

namespace SPSS.Domain.Common;

public sealed class Response<TData, TError>
{
    public HttpStatusCode Status { get; set; }
    public TData? Data { get; set; }
    public TError? Error { get; set; }
}

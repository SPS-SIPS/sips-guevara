namespace SPSS.API.Filters;
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation(
            "Incoming request {method} {path} from IP: {ipAddress}",
            context.Request.Method,
            context.Request.Path,
            ipAddress);

        await _next(context);
    }
}

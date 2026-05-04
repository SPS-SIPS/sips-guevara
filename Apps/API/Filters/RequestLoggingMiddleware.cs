namespace SPSS.API.Filters;
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context?.Connection?.RemoteIpAddress?.ToString();
        _logger.LogError("Incoming request from IP: {ipAddress}, body: {body}", ipAddress, context?.Request.Body);
        await _next(context!);
    }
}
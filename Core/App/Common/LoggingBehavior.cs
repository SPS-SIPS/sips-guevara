namespace SPSS.App.Common;
public sealed class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger, IIdentityService identity) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger = logger;
    private readonly IIdentityService _identity = identity;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var name = message.GetType()?.Name;
        if (_identity.IsAuthenticated)
        {
            var username = _identity.Username;
            _logger.LogInformation("Request: {Name} username: {username}", name, username);
        }
        else
        {
            _logger.LogInformation("Request: {Name} username: not authenticated", name);
        }
        return await next(message, cancellationToken);
    }
}

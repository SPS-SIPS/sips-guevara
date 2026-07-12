namespace SPSS.Infrastructure.Workers;

public sealed class OracleFailedTransactionNotificationJob(
    IScheduleConfig<OracleFailedTransactionNotificationJob> config,
    ILogger<OracleFailedTransactionNotificationJob> logger,
    IServiceProvider services,
    TimeProvider timeProvider) : CronJobService(config.CronExpression!, config.TimeZoneInfo!)
{
    private readonly ILogger<OracleFailedTransactionNotificationJob> _logger = logger;
    private readonly IServiceProvider _services = services;
    private readonly TimeProvider _timeProvider = timeProvider;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Oracle failed transaction notification job started, next occurrence will be at: {TimeStamp} Utc",
            GetSchedule);

        return base.StartAsync(cancellationToken);
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var notificationConfig = scope.ServiceProvider.GetRequiredService<OracleFailedTransactionNotificationConfig>();

        if (!notificationConfig.Enabled)
        {
            _logger.LogInformation("Oracle failed transaction notification job is disabled.");
            return;
        }

        var notifier = scope.ServiceProvider.GetRequiredService<IOracleFailedTransactionNotificationService>();
        var now = _timeProvider.GetUtcNow();

        _logger.LogInformation(
            "Oracle failed transaction notification job checking for the next completed report window at {Now}.",
            now);

        await notifier.NotifyNextWindowAsync(now, cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Oracle failed transaction notification job stopped.");
        return base.StopAsync(cancellationToken);
    }
}

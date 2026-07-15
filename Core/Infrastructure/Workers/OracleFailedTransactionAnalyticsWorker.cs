using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SPSS.Infrastructure.Workers;

public sealed class OracleFailedTransactionAnalyticsWorker(
    ILogger<OracleFailedTransactionAnalyticsWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OracleFailedTransactionAnalyticsConfig> options) : BackgroundService
{
    private readonly ILogger<OracleFailedTransactionAnalyticsWorker> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly OracleFailedTransactionAnalyticsConfig _config = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Oracle failed transaction analytics worker is disabled.");
            return;
        }

        _logger.LogInformation(
            "Oracle failed transaction analytics worker started. Interval={IntervalHours}h, Lookback={LookbackDays}d",
            _config.RunIntervalHours,
            _config.LookbackDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            await GenerateAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(Math.Max(1, _config.RunIntervalHours)), stoppingToken);
        }
    }

    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IOracleFailedTransactionAnalyticsService>();
            var runId = await service.GenerateAsync(cancellationToken);

            _logger.LogInformation("Oracle failed transaction analytics worker completed. RunId={RunId}", runId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Oracle failed transaction analytics worker is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oracle failed transaction analytics worker failed.");
        }
    }
}

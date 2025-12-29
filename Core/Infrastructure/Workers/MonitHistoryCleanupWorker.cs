using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SPSS.Infrastructure.Workers;

public sealed class MonitHistoryCleanupConfiguration
{
    public int RetentionDays { get; set; } = 7;
    public int RunIntervalHours { get; set; } = 24;
    public int BatchSize { get; set; } = 50000;
}

public sealed class MonitHistoryCleanupWorker(
    ILogger<MonitHistoryCleanupWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<MonitHistoryCleanupConfiguration> options) : BackgroundService
{
    private readonly ILogger<MonitHistoryCleanupWorker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly MonitHistoryCleanupConfiguration _config = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public bool IsHealthy { get; private set; } = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MonitHistory Cleanup Worker started. Retention: {RetentionDays} days, Interval: {IntervalHours} hours",
            _config.RetentionDays,
            _config.RunIntervalHours);

        stoppingToken.Register(() => _logger.LogInformation("MonitHistory Cleanup Worker is stopping..."));

        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupOldRecordsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(_config.RunIntervalHours), stoppingToken);
                IsHealthy = true;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MonitHistory Cleanup Worker task was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in MonitHistory Cleanup Worker.");
            IsHealthy = false;
        }
        finally
        {
            _logger.LogInformation("MonitHistory Cleanup Worker has stopped.");
        }
    }

    private async Task CleanupOldRecordsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var storageBroker = scope.ServiceProvider.GetRequiredService<IStorageBroker>();

            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-_config.RetentionDays);
            var totalDeleted = 0;

            _logger.LogInformation(
                "Starting cleanup of MonitHistory records older than {CutoffDate} (retention: {RetentionDays} days)",
                cutoffDate,
                _config.RetentionDays);

            while (!cancellationToken.IsCancellationRequested)
            {
                var idsToDelete = await storageBroker.MonitHistories
                    .AsNoTracking()
                    .Where(x => x.CheckedAt < cutoffDate)
                    .OrderBy(x => x.Id)
                    .Take(_config.BatchSize)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (idsToDelete.Count == 0)
                {
                    break;
                }

                await storageBroker.MonitHistories
                    .Where(x => idsToDelete.Contains(x.Id))
                    .ExecuteDeleteAsync(cancellationToken);

                totalDeleted += idsToDelete.Count;

                _logger.LogInformation(
                    "Deleted batch of {BatchCount} records. Total deleted so far: {TotalDeleted}",
                    idsToDelete.Count,
                    totalDeleted);

                if (idsToDelete.Count < _config.BatchSize)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation(
                    "Cleanup completed. Total records deleted: {TotalDeleted}",
                    totalDeleted);
            }
            else
            {
                _logger.LogInformation("No old records found to delete.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MonitHistory cleanup");
            IsHealthy = false;
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MonitHistory Cleanup Worker is stopping.");
        IsHealthy = false;
        await base.StopAsync(stoppingToken);
    }
}

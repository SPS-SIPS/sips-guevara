using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SPSS.Infrastructure.Workers;

public sealed class TransactionRetentionCleanupConfiguration
{
    public bool Enabled { get; set; } = true;
    public int ProcessedTransactionsRetentionHours { get; set; } = 48;
    public int FailedTransactionsRetentionDays { get; set; } = 30;
    public int RunIntervalHours { get; set; } = 1;
    public int BatchSize { get; set; } = 50000;
}

public sealed class TransactionRetentionCleanupWorker(
    ILogger<TransactionRetentionCleanupWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<TransactionRetentionCleanupConfiguration> options) : BackgroundService
{
    private readonly ILogger<TransactionRetentionCleanupWorker> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly TransactionRetentionCleanupConfiguration _config = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Transaction retention cleanup worker is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(Math.Max(1, _config.RunIntervalHours)), stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageBroker>();
            var now = DateTimeOffset.UtcNow;
            var processedCutoff = now.AddHours(-Math.Max(1, _config.ProcessedTransactionsRetentionHours));
            var failedCutoff = now.AddDays(-Math.Max(1, _config.FailedTransactionsRetentionDays));
            var batchSize = Math.Max(100, _config.BatchSize);

            var processedIds = await storage.ProcessedTransactions
                .Where(x => x.ReceivedAt < processedCutoff)
                .OrderBy(x => x.ReceivedAt)
                .Take(batchSize)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var failedIds = await storage.FailedTransactions
                .Where(x => x.CreatedDate != null && x.CreatedDate < failedCutoff)
                .OrderBy(x => x.CreatedDate)
                .Take(batchSize)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (processedIds.Count > 0)
            {
                await storage.ProcessedTransactions
                    .Where(x => processedIds.Contains(x.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (failedIds.Count > 0)
            {
                await storage.FailedTransactions
                    .Where(x => failedIds.Contains(x.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (processedIds.Count > 0 || failedIds.Count > 0)
            {
                _logger.LogInformation(
                    "Transaction retention cleanup completed. DeletedProcessed={DeletedProcessed}, DeletedFailed={DeletedFailed}, ProcessedCutoff={ProcessedCutoff}, FailedCutoff={FailedCutoff}",
                    processedIds.Count,
                    failedIds.Count,
                    processedCutoff,
                    failedCutoff);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Transaction retention cleanup worker is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction retention cleanup failed.");
        }
    }
}

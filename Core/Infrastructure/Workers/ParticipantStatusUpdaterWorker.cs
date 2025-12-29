using Microsoft.Extensions.Hosting;
using SPSS.Domain.Models.BL;

namespace SPSS.Infrastructure.Workers;

public sealed class ParticipantStatusUpdaterWorker(
    ILogger<ParticipantStatusUpdaterWorker> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly ILogger<ParticipantStatusUpdaterWorker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private const int CheckIntervalMinutes = 2;
    private const int ConsecutiveFailuresThreshold = 3;
    private const int ConsecutiveSuccessesThreshold = 2;

    public bool IsHealthy { get; private set; } = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Participant Status Updater Worker started");
        stoppingToken.Register(() => _logger.LogInformation("Participant Status Updater Worker is stopping..."));

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdateParticipantStatusesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);
                IsHealthy = true;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Participant Status Updater Worker task was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in Participant Status Updater Worker.");
            IsHealthy = false;
        }
        finally
        {
            _logger.LogInformation("Participant Status Updater Worker has stopped.");
        }
    }

    private async Task UpdateParticipantStatusesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var storageBroker = scope.ServiceProvider.GetRequiredService<IStorageBroker>();

            var cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-10);
            
            var recentChecks = await storageBroker.MonitHistories
                .AsNoTracking()
                .Where(x => x.CheckedAt >= cutoffTime)
                .GroupBy(x => x.InstitutionBic)
                .Select(g => new
                {
                    InstitutionBic = g.Key,
                    InstitutionName = g.OrderByDescending(x => x.CheckedAt).First().InstitutionName,
                    LastCheckedAt = g.Max(x => x.CheckedAt),
                    RecentChecks = g.OrderByDescending(x => x.CheckedAt).Take(5).Select(x => new
                    {
                        x.Success,
                        x.CheckedAt,
                        x.Error
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;

            foreach (var check in recentChecks)
            {
                var existingStatus = await storageBroker.ParticipantStatuses
                    .FirstOrDefaultAsync(x => x.InstitutionBic == check.InstitutionBic, cancellationToken);

                var consecutiveFailures = 0;
                var consecutiveSuccesses = 0;
                var lastError = string.Empty;

                foreach (var recentCheck in check.RecentChecks)
                {
                    if (recentCheck.Success)
                    {
                        consecutiveSuccesses++;
                        if (consecutiveFailures > 0) break;
                    }
                    else
                    {
                        consecutiveFailures++;
                        if (string.IsNullOrEmpty(lastError))
                        {
                            lastError = recentCheck.Error ?? "Unknown error";
                        }
                        if (consecutiveSuccesses > 0) break;
                    }
                }

                var isLive = DetermineIsLive(consecutiveSuccesses, consecutiveFailures, existingStatus?.IsLive ?? false);

                if (existingStatus == null)
                {
                    var newStatus = new ParticipantStatus
                    {
                        InstitutionBic = check.InstitutionBic,
                        InstitutionName = check.InstitutionName,
                        IsLive = isLive,
                        LastCheckedAt = check.LastCheckedAt,
                        LastStatusChangeAt = now,
                        ConsecutiveFailures = consecutiveFailures,
                        ConsecutiveSuccesses = consecutiveSuccesses,
                        LastError = lastError,
                        UpdatedAt = now
                    };

                    await storageBroker.ParticipantStatuses.AddAsync(newStatus, cancellationToken);
                }
                else
                {
                    var statusChanged = existingStatus.IsLive != isLive;

                    existingStatus.InstitutionName = check.InstitutionName;
                    existingStatus.IsLive = isLive;
                    existingStatus.LastCheckedAt = check.LastCheckedAt;
                    existingStatus.ConsecutiveFailures = consecutiveFailures;
                    existingStatus.ConsecutiveSuccesses = consecutiveSuccesses;
                    existingStatus.LastError = lastError;
                    existingStatus.UpdatedAt = now;

                    if (statusChanged)
                    {
                        existingStatus.LastStatusChangeAt = now;
                        _logger.LogInformation(
                            "Participant {Bic} status changed to {Status}",
                            check.InstitutionBic,
                            isLive ? "LIVE" : "OFFLINE");
                    }

                    storageBroker.ParticipantStatuses.Update(existingStatus);
                }
            }

            await storageBroker.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated status for {Count} participants", recentChecks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating participant statuses");
        }
    }

    private static bool DetermineIsLive(int consecutiveSuccesses, int consecutiveFailures, bool currentStatus)
    {
        if (consecutiveSuccesses >= ConsecutiveSuccessesThreshold)
        {
            return true;
        }

        if (consecutiveFailures >= ConsecutiveFailuresThreshold)
        {
            return false;
        }

        return currentStatus;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Participant Status Updater Worker is stopping.");
        IsHealthy = false;
        await base.StopAsync(stoppingToken);
    }
}

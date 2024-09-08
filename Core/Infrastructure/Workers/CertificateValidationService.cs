using Microsoft.Extensions.Hosting;
namespace SPSS.Infrastructure.Workers;

public class CertificateValidationService(ILogger<CertificateValidationService> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly ILogger<CertificateValidationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    public bool IsHealthy { get; private set; } = true;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Certificate Validation Service cycle...");
        stoppingToken.Register(() => _logger.LogInformation("Stopping Certificate Validation Service cycle..."));

        try
        {
            _logger.LogInformation("Running Certificate Validation Service cycle...");
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var certificateValidator = scope.ServiceProvider.GetRequiredService<CertificateValidator>();

                await certificateValidator.ValidateCertificates(stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                _logger.LogInformation("Completing Certificate Validation Service cycle...");
                IsHealthy = true;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Certificate Validation Service task was cancelled.");
        }
        catch (Exception ex)
        {
            // Log unexpected exceptions
            _logger.LogError(ex, "An error occurred in Certificate Validation Service.");
            IsHealthy = false;
        }
        finally
        {
            _logger.LogInformation("Certificate Validation Service has stopped.");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Certificate Validation Service is stopping.");
        IsHealthy = false;
        await base.StopAsync(stoppingToken);
    }
}

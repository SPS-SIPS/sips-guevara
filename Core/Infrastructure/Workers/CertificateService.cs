using Microsoft.Extensions.Hosting;
namespace SPSS.Infrastructure.Workers;

public class CertificateService(ILogger<CertificateService> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly ILogger<CertificateService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    public bool IsHealthy { get; private set; } = true;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Certificate Service cycle...");
        stoppingToken.Register(() => _logger.LogInformation("Stopping Certificate Service cycle..."));

        try
        {
            _logger.LogInformation("Running Certificate Service cycle...");
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var certificateRetriever = scope.ServiceProvider.GetRequiredService<ICertificateRetriever>();
                var certificateValidator = scope.ServiceProvider.GetRequiredService<CertificateValidator>();

                certificateRetriever.GetAllCertificates();
                await certificateValidator.ValidateCertificates(stoppingToken);

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                _logger.LogInformation("Completing Certificate Service cycle...");
                IsHealthy = true;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Certificate Service task was cancelled.");
        }
        catch (Exception ex)
        {
            // Log unexpected exceptions
            _logger.LogError(ex, "An error occurred in Certificate Service.");
            IsHealthy = false;
        }
        finally
        {
            _logger.LogInformation("Certificate Service has stopped.");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Certificate Service is stopping.");
        IsHealthy = false;
        await base.StopAsync(stoppingToken);
    }
}

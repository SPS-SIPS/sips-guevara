using AutoMapper;
using SPSS.Domain.Interfaces;

namespace SPSS.Infrastructure.Workers;
public class NSIMonitoring(IScheduleConfig<NSIMonitoring> config, ILogger<NSIMonitoring> logger, IServiceProvider services) : CronJobService(config.CronExpression!, config.TimeZoneInfo!)
{
    private readonly ILogger<NSIMonitoring> _logger = logger;
    private readonly IServiceProvider _services = services;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("NSI Monitoring Job Started, next occurrence will be at: {timeStamp} Utc", GetSchedule);
        return base.StartAsync(cancellationToken);
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        await Task.Delay(2, cancellationToken);

        _logger.LogInformation("NSI Monitoring Job: is working...");
        using var scope = _services.CreateScope();
        var nsi = scope.ServiceProvider.GetRequiredService<NSI>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageBroker>();
        var statistics = scope.ServiceProvider.GetRequiredService<IBicStatisticsService>();
        var notifier = scope.ServiceProvider.GetRequiredService<INSIEmailNotificationService>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var input = Path.Combine(nsi.Directory, "input");
        var output = Path.Combine(nsi.Directory, "output");
        var backup = Path.Combine(nsi.Directory, "backup");
        try
        {
            _logger.LogInformation("NSI Monitoring Job: current input: {input}...", input);

            // check if the input directory exists or not
            if (!Directory.Exists(input))
            {
                Directory.CreateDirectory(input);
                _logger.LogError("NSI Monitoring Job: Input Directory does not exist");
                return;
            }

            // check if the output directory exists or not
            if (!Directory.Exists(output))
            {
                // create the output directory
                Directory.CreateDirectory(output);
            }

            var files = Directory.GetFiles(input, "*.rtgs.xml");

            // Check if there are any files in the input directory
            if (files.Length == 0)
            {
                _logger.LogInformation("NSI Monitoring Job: No files found in the input directory");
                return;
            }

            foreach (var file in files)
            {
                var msgId = await NSIService.ImportXmlToDbAsync(file, storage, cancellationToken);

                if (msgId == null)
                {
                    _logger.LogError("NSI Monitoring Job: Failed to import file {file}", file);
                    continue;
                }

                // sleep for 2 seconds to avoid overloading the database
                await Task.Delay(2000, cancellationToken);

                await NSIService.GenerateRTGSFiles(storage, msgId.Value, output, nsi.FeeTTC!, nsi.TxnTTC!, _logger, cancellationToken);

                // sleep for 2 seconds to avoid overloading the database
                await Task.Delay(2000, cancellationToken);

                await NSIService.GenerateStatisticsAsync(storage, msgId.Value, statistics, mapper, cancellationToken);

                // Notify participants via email with reconciliation CSV attachments per institution
                await notifier.NotifyAsync(msgId.Value, cancellationToken);

                var fileName = Path.GetFileName(file);
                var destination = Path.Combine(backup, fileName);
                File.Move(file, destination);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("NSI Monitoring Job: Error while fetching NSI File status {Exception}", ex);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"NIS Monitoring Job stopped...");
        return base.StopAsync(cancellationToken);
    }

}
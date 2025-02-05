namespace SPSS.Infrastructure.Workers;
public class NSIMonitoring(IScheduleConfig<NSIMonitoring> config, ILogger<NSIMonitoring> logger, IServiceProvider services) : CronJobService(config.CronExpression!, config.TimeZoneInfo!)
{
    private readonly ILogger<NSIMonitoring> _logger = logger;
    private readonly IServiceProvider _services = services;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("VPN Monitoring Job Started, next occurrence will be at: {timeStamp} Utc", GetSchedule);
        return base.StartAsync(cancellationToken);
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        await Task.Delay(2, cancellationToken);

        _logger.LogInformation("NSI Monitoring Job is working...");
        using var scope = _services.CreateScope();
        var nsi = scope.ServiceProvider.GetRequiredService<NSI>();
        var input = Path.Combine(nsi.Directory, "input");
        var output = Path.Combine(nsi.Directory, "output");
        var backup = Path.Combine(nsi.Directory, "backup");
        try
        {
            _logger.LogDebug("NSI Monitoring Job current directory: {directory}...", nsi.Directory);

            // check if the input directory exists or not
            if (!Directory.Exists(input))
            {
                Directory.CreateDirectory(input);
                _logger.LogError("Input Directory does not exist");
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
                _logger.LogInformation("No files found in the input directory");
                return;
            }

            foreach (var file in files)
            {
                // check if generateFeesFile is true and seperate both files.
                // (string fees, string txn) = NSIHelpers.Generate(
                //     inputFile: fileName,
                //     outputDir: outputDirectory);
                ConvertNSIToCSV.Detail(inputFile: file, sheetDir: output);
                ConvertNSIToCSV.Summary(inputFile: file, sheetDir: output);

                // Move the file to the backup directory
                var fileName = Path.GetFileName(file);
                var destination = Path.Combine(backup, fileName);
                File.Move(file, destination);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while fetching NSI File status {Exception}", ex);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"NIS Monitoring Job stopped...");
        return base.StopAsync(cancellationToken);
    }

}
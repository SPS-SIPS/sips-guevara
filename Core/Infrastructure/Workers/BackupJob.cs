using System.Diagnostics;
using Npgsql;

namespace SPSS.Infrastructure.Workers;
public class BackupJob(IScheduleConfig<BackupJob> config, ILogger<BackupJob> logger, IConfiguration configuration, BackupConfiguration backupConfiguration) : CronJobService(config.CronExpression!, config.TimeZoneInfo!)
{
    private readonly ILogger<BackupJob> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly BackupConfiguration _backupConfiguration = backupConfiguration;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Backup Job Started, next occurrence will be at: {timeStamp} Utc", GetSchedule);
        return base.StartAsync(cancellationToken);
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Backup Job is working...");
        try
        {
            string connectionString = _configuration.GetConnectionString("db")!;
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            string db = conn.Database;
            string backupPath = Path.Combine(_backupConfiguration.Path!, $"{db}_{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss}.backup");

            using var cmd = new NpgsqlCommand();
            cmd.Connection = conn;



            var process = new Process();
            if (_backupConfiguration.UseAuth)
            {
                cmd.CommandText = $"pg_dump -F c -b -v -f \"{backupPath}\" {db}";
                process.StartInfo.Environment.Add("PGPASSWORD", _backupConfiguration.Password!);
                process.StartInfo.Environment.Add("PGUSER", _backupConfiguration.Username!);
            }
            else
            {
                cmd.CommandText = $"pg_dump -F c -b -v -f \"{backupPath}\" \"{db}\"";
            }
            var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            process.StartInfo.FileName = isWindows ? "cmd" : "bash";
            process.StartInfo.Arguments = $"-c \"{cmd.CommandText}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                Console.WriteLine("Backup created successfully.");
            }
            else
            {
                Console.WriteLine($"Backup failed with error: {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup Job failed");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Backup Job stopped...");
        return base.StopAsync(cancellationToken);
    }

}
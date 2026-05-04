namespace SPSS.Domain.Extensions;
public static class ScheduledServiceExtensions
{
    public static IServiceCollection AddCronJob<T>(this IServiceCollection services, JobConfiguration option) where T : CronJobService
    {
        if (option == null)
        {
            throw new ArgumentNullException(nameof(option), @"Please provide Schedule Configurations.");
        }
        var config = new ScheduleConfig<T>();
        if (string.IsNullOrWhiteSpace(option.Expression))
        {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            throw new ArgumentNullException(nameof(ScheduleConfig<T>.CronExpression), @"Empty Cron Expression is not allowed.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }

        config.CronExpression = option.Expression;
        config.TimeZoneInfo = option.TimeZoneInfo == "Utc" ? TimeZoneInfo.Utc : TimeZoneInfo.Local;

        services.AddSingleton<IScheduleConfig<T>>(config);
        services.AddHostedService<T>();
        return services;
    }
}

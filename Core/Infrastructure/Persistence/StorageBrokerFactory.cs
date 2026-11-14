using Microsoft.EntityFrameworkCore.Design;

namespace SPSS.Infrastructure.Persistence;

public sealed class StorageBrokerFactory : IDesignTimeDbContextFactory<StorageBroker>
{
    public StorageBroker CreateDbContext(string[] args)
    {
        string connectionString;
        if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
        {
            connectionString = args[0];
        }
        else
        {
            connectionString = Environment.GetEnvironmentVariable("SPSS_DESIGN_TIME_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Set SPSS_DESIGN_TIME_CONNECTION_STRING or provide a connection string to the design-time factory.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<StorageBroker>();
        optionsBuilder.UseNpgsql(connectionString).UseLowerCaseNamingConvention();

        return new StorageBroker(optionsBuilder.Options);
    }
}
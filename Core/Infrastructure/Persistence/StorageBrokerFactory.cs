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
            connectionString = "Server=localhost;Port=5432;Database=spss;User Id=postgres;Password=REDACTED_SECRET;";
        }

        var optionsBuilder = new DbContextOptionsBuilder<StorageBroker>();
        optionsBuilder.UseNpgsql(connectionString).UseLowerCaseNamingConvention();

        return new StorageBroker(optionsBuilder.Options);
    }
}
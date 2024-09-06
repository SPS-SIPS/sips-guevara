namespace SPSS.API.Jobs;
public class InvalidateExpireTokens(IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IStorageBroker>();
            var date = DateTime.UtcNow;

            var query = context.Tokens.OrderBy(x => x.Id).Where(x => x.ExpiresOn <= date && !x.Invalidated);
            var count = await query.CountAsync(stoppingToken);

            var size = 10;
            var pages = (int)Math.Ceiling((decimal)count / size);
            for (var i = 0; i < pages; i++)
            {
                var skip = i * size;
                await query.Skip(skip).Take(size).ForEachAsync(x =>
                {
                    x.Invalidated = true;
                }, stoppingToken);

                await context.SaveChangesAsync(stoppingToken);
            }
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}

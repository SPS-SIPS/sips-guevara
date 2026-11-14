using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using SPSS.Domain.Enums;
using Microsoft.Extensions.Logging;
using Claim = SPSS.Domain.Models.Identity.Claim;
namespace SPSS.Infrastructure.Persistence;

public sealed partial class StorageBrokerInitializer(ILogger<StorageBrokerInitializer> logger, IStorageBroker broker, IPasswordHash hash, IConfiguration configuration) : IStorageBrokerInitializer
{
    private readonly ILogger<StorageBrokerInitializer> _logger = logger;
    private readonly IStorageBroker _broker = broker;
    private readonly IPasswordHash _hash = hash;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _broker.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TrySeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync(CancellationToken cancellationToken = default)
    {
        var systemClaims = typeof(KnownClaims).GetFields(BindingFlags.Public | BindingFlags.Static)
                   .Select(x => new Claim
                   {
                       Type = x?.GetValue(null)?.ToString()!,
                       // Format name by adding spaces between capital letters
                       Description = MyRegex().Replace(x?.Name!, " $1").Trim(),
                   }).ToList();

        var dbClaims = await _broker.Claims.ToListAsync(cancellationToken: cancellationToken);
        // compare claims with db claims and add new claims
        _broker.Claims.AddRange(systemClaims.Where(x => !dbClaims.Any(y => y.Type.Equals(x.Type))));
        await _broker.SaveChangesAsync(cancellationToken);

        if (!await _broker.Roles.AnyAsync(x => x.Name.Equals("Admin"), cancellationToken: cancellationToken))
        {
            var claims = await _broker.Claims.ToListAsync(cancellationToken: cancellationToken);
            Role role = new()
            {
                Name = "Admin",
                Active = true,
                Super = true,
                RoleClaims = claims.Select(x => new RoleClaim { Type = x.Type }).ToList()
            };
            await _broker.Roles.AddAsync(role, cancellationToken);
            await _broker.SaveChangesAsync(cancellationToken);
        }
        if (!_broker.Users.Any(x => x.Username.Equals("admin")))
        {
            var bootstrapPassword = configuration["Bootstrap:AdminPassword"];
            if (!configuration.GetValue<bool>("Bootstrap:Enabled"))
            {
                _logger.LogWarning("No administrator exists. Use the explicit bootstrap procedure to create one.");
                return;
            }
            if (string.IsNullOrWhiteSpace(bootstrapPassword) || bootstrapPassword.Length < 16)
                throw new InvalidOperationException("Bootstrap:AdminPassword must contain at least 16 characters.");
            var masterRole = _broker.Roles.SingleOrDefault(x => x.Name.Equals("Admin"));
            var user = new User
            {
                Username = "admin",
                Phone = "01",
                Active = true,
                SecurityStamp = "bootstrap:" + Guid.NewGuid().ToString(),
                Email = "admin@sps.com",
            };
            user.Roles.Add(new UserRole
            {
                RoleId = masterRole!.Id
            });
            user.PasswordHash = _hash.HashPassword(user, bootstrapPassword);

            await _broker.Users.AddAsync(user, cancellationToken);
            await _broker.SaveChangesAsync(cancellationToken);
        }


    }

    [GeneratedRegex("([A-Z])")]
    private static partial Regex MyRegex();
}
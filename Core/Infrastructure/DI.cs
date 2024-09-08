using System.Text;
using Microsoft.IdentityModel.Tokens;
using SPSS.Infrastructure.Identity.Services;
using SPSS.Infrastructure.Persistence;
using SPSS.Infrastructure.Persistence.Interceptors;
namespace Infrastructure;
public static class DI
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddScoped<SoftDeleteEntityInterceptor>();
        services.AddScoped<VersionedEntityInterceptor>();

        var connectionString = configuration.GetConnectionString("db");
        connectionString = connectionString?.Replace("{{PASSWORD}}", Environment.GetEnvironmentVariable("POSTGRESQL_PASSWORD"));

        services.AddDbContext<StorageBroker>((sp, options) =>
        {
            options.UseNpgsql(connectionString).UseLowerCaseNamingConvention();
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<SoftDeleteEntityInterceptor>());
            options.AddInterceptors(sp.GetRequiredService<VersionedEntityInterceptor>());
        });

        services.AddScoped<IStorageBrokerInitializer, StorageBrokerInitializer>();
        services.AddScoped<IStorageBroker>(provider => provider.GetService<StorageBroker>()!);
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddSingleton<IRolesCache, RolesCache>();
        services.AddSingleton<IPasswordHash, PasswordHash>();
        services.AddSingleton<IPasswordManager, PasswordManager>();
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<ICertificateRetriever, LDAPCertificateFetcher>();
        services.AddScoped<IFileStorage, FileService>();

        var jwtSettings = new JwtSettings();
        configuration.Bind(nameof(JwtSettings), jwtSettings);
        services.AddSingleton(jwtSettings);

        var appSettings = new AppSettings();
        configuration.Bind(nameof(AppSettings), appSettings);
        services.AddSingleton(appSettings);

        var emailConfig = new EmailConfig();
        configuration.Bind(nameof(EmailConfig), emailConfig);
        services.AddSingleton(emailConfig);

        var backupConfiguration = new BackupConfiguration();
        configuration.Bind("Jobs:Backup", backupConfiguration);
        services.AddSingleton(backupConfiguration);

        var TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            ClockSkew = TimeSpan.FromSeconds(5)
        };

        // services.AddCronJob<BackupJob>(new JobConfiguration { Expression = backupConfiguration.Expression, TimeZoneInfo = backupConfiguration.TimeZoneInfo });
        services.AddHostedService<CertificateService>();
        services.AddSingleton(TokenValidationParameters);
        return services;
    }
}

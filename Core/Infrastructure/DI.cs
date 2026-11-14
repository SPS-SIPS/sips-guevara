using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SPSS.Domain.Extensions;
using SPSS.Infrastructure;
using SPSS.Domain.Interfaces;
using SPSS.Domain.Services;
using SPSS.Infrastructure.Services;
using SPSS.Infrastructure.Identity.Services;
using SPSS.Infrastructure.Persistence;
using SPSS.Infrastructure.Persistence.Interceptors;
namespace Infrastructure;
public static class DI
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var isOpenApiExport = configuration.GetValue<bool>("OpenApiExport");

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
        services.AddSingleton<ITemplateRenderer, TemplateRendererService>();

        services.AddScoped<INSIEmailNotificationService, NSIEmailNotificationService>(); // Added registration
        services.AddScoped<IOracleFailedTransactionNotificationService, OracleFailedTransactionNotificationService>();
        services.AddScoped<IOracleFailedTransactionAnalyticsService, OracleFailedTransactionAnalyticsService>();
        services.AddScoped<INSIGeneratorService, NSIGeneratorService>();

        services.AddScoped<ICertificateRetriever, LDAPCertificateFetcher>();
        services.AddScoped<CertificateValidator>();
        services.AddScoped<CertificateExpirationNotifier>();
        services.AddScoped<IFileStorage, FileService>();
        services.AddScoped<IBicStatisticsService, BicStatisticsService>();
        services.AddSingleton<IStatisticsReportService, StatisticsReportService>();
        services.AddScoped<IBalanceThresholdService, BalanceThresholdService>();

        var jwtSettings = new JwtSettings();
        configuration.Bind(nameof(JwtSettings), jwtSettings);
        if (!isOpenApiExport && (Encoding.UTF8.GetByteCount(jwtSettings.Secret) < 32 || string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
            string.IsNullOrWhiteSpace(jwtSettings.Audience) || jwtSettings.TokenLifetime <= TimeSpan.Zero || jwtSettings.TokenLifetime > TimeSpan.FromMinutes(30)))
            throw new InvalidOperationException("Configure a JWT signing secret of at least 32 bytes, issuer, audience and an access lifetime of at most 30 minutes.");
        services.AddSingleton(jwtSettings);

        var appSettings = new AppSettings();
        configuration.Bind(nameof(AppSettings), appSettings);
        services.AddSingleton(appSettings);

        var emailConfig = new EmailConfig();
        configuration.Bind(nameof(EmailConfig), emailConfig);
        services.AddSingleton(emailConfig);

        var oracleFailedTransactionNotificationConfig = new OracleFailedTransactionNotificationConfig();
        configuration.Bind("OracleFailedTransactionNotifications", oracleFailedTransactionNotificationConfig);
        services.AddSingleton(oracleFailedTransactionNotificationConfig);

        var oracleFailedTransactionAnalyticsConfig = new OracleFailedTransactionAnalyticsConfig();
        configuration.Bind("OracleFailedTransactionAnalytics", oracleFailedTransactionAnalyticsConfig);
        services.AddSingleton(oracleFailedTransactionAnalyticsConfig);
        services.Configure<OracleFailedTransactionAnalyticsConfig>(configuration.GetSection("OracleFailedTransactionAnalytics"));

        var certificateExpirationNotificationConfig = new CertificateExpirationNotificationConfig();
        configuration.Bind("CertificateExpirationNotifications", certificateExpirationNotificationConfig);
        services.AddSingleton(certificateExpirationNotificationConfig);

        var backupConfiguration = new BackupConfiguration();
        configuration.Bind("Jobs:Backup", backupConfiguration);
        services.AddSingleton(backupConfiguration);

        var nsiConfig = new NSI();
        configuration.Bind(nameof(NSI), nsiConfig);
        services.AddSingleton(nsiConfig);
        if (!isOpenApiExport)
        {
            CreateHome(nsiConfig);
        }

        services.Configure<MonitHistoryCleanupConfiguration>(configuration.GetSection("MonitHistoryCleanup"));
        services.Configure<TransactionRetentionCleanupConfiguration>(configuration.GetSection("TransactionRetentionCleanup"));

        var TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            ClockSkew = TimeSpan.FromSeconds(5)
        };

        if (!isOpenApiExport)
        {
            var backupJob = new JobConfiguration { Expression = backupConfiguration.Expression, TimeZoneInfo = backupConfiguration.TimeZoneInfo };
            if (HasCronExpression(backupJob))
            {
                services.AddCronJob<BackupJob>(backupJob);
            }

            var nsi = new JobConfiguration();
            configuration.Bind("Jobs:NSI", nsi);
            if (HasCronExpression(nsi))
            {
                services.AddCronJob<NSIMonitoring>(nsi);
            }

            var oracleFailedTransactionNotifications = new JobConfiguration();
            configuration.Bind("Jobs:OracleFailedTransactionNotifications", oracleFailedTransactionNotifications);
            if (oracleFailedTransactionNotificationConfig.Enabled && HasCronExpression(oracleFailedTransactionNotifications))
            {
                services.AddCronJob<OracleFailedTransactionNotificationJob>(oracleFailedTransactionNotifications);
            }

            services.AddHostedService<CertificateFetchingService>();
            services.AddHostedService<CertificateValidationService>();
            services.AddHostedService<ParticipantStatusUpdaterWorker>();
            services.AddHostedService<MonitHistoryCleanupWorker>();
            services.AddHostedService<TransactionRetentionCleanupWorker>();
            if (oracleFailedTransactionAnalyticsConfig.Enabled)
            {
                services.AddHostedService<OracleFailedTransactionAnalyticsWorker>();
            }
        }
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = TokenValidationParameters;
                options.MapInboundClaims = false;
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var options = new DbContextOptionsBuilder<StorageBroker>()
                            .UseNpgsql(connectionString).UseLowerCaseNamingConvention().Options;
                        await using var storage = new StorageBroker(options);
                        if (context.Principal is null || !await SessionValidation.ValidateAsync(storage, context.Principal, context.HttpContext.RequestAborted))
                            context.Fail("Session is no longer valid.");
                        else if (context.Principal.FindFirstValue("security_stamp")?.StartsWith("bootstrap:", StringComparison.Ordinal) == true &&
                                 !context.Request.Path.Equals(new PathString("/api/v1/Auth/changePassword")))
                            context.Fail("The bootstrap password must be changed.");
                    }
                };
            });

        services.AddSingleton(TokenValidationParameters);

        var apiAuthConfig = configuration.GetSection("ApiAuth").Get<List<ApiClient>>() ?? [];
        services.AddSingleton(apiAuthConfig);

        return services;
    }

    private static bool HasCronExpression(JobConfiguration configuration)
        => !string.IsNullOrWhiteSpace(configuration.Expression);

    private static void CreateHome(NSI file)
    {
        if (string.IsNullOrWhiteSpace(file.Directory))
        {
            return;
        }

        Directory.CreateDirectory(file.Directory);
        Directory.CreateDirectory(Path.Combine(file.Directory, "input"));
        Directory.CreateDirectory(Path.Combine(file.Directory, "output"));
        Directory.CreateDirectory(Path.Combine(file.Directory, "backup"));
    }
}

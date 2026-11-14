using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using SPSS.API.Installers;
using SPSS.API.Jobs;
using Serilog;
using static SPSS.Domain.Extensions.InitializerExtensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
var isOpenApiExport = builder.Configuration.GetValue<bool>("OpenApiExport");

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var proxy in builder.Configuration.GetSection("Security:TrustedProxies").Get<string[]>() ?? [])
        options.KnownProxies.Add(IPAddress.Parse(proxy));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("reports", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 1, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
});
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    var dataProtectionBuilder = builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
        .SetApplicationName(builder.Configuration["DataProtection:ApplicationName"] ?? builder.Environment.ApplicationName);

    var dataProtectionCertificatePath = builder.Configuration["DataProtection:CertificatePath"];
    if (!string.IsNullOrWhiteSpace(dataProtectionCertificatePath))
    {
        var dataProtectionCertificatePassword = builder.Configuration["DataProtection:CertificatePassword"];
        dataProtectionBuilder.ProtectKeysWithCertificate(new X509Certificate2(
            dataProtectionCertificatePath,
            dataProtectionCertificatePassword));
    }
}

builder.Host.UseSerilog((context, configuration) =>
{
    try
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext();
    }
    catch (Exception ex) when (ex is InvalidOperationException or FormatException or ArgumentException)
    {
        Console.Error.WriteLine($"Serilog configuration is invalid; falling back to console logging. {ex.Message}");

        configuration
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext();
    }
});

builder.Services.AddHttpClient();
builder.Services.InstallerServicesInAssembly(builder.Configuration);
if (!isOpenApiExport)
{
    builder.Services.AddHostedService<InvalidateExpireTokens>();
}

var app = builder.Build();
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    if (!isOpenApiExport)
    {
        await app.InitializeDatabaseAsync();
    }
    app.UseHttpsRedirection();
    var swaggerConfig = new SwaggerConfig();
    builder.Configuration.Bind(nameof(SwaggerConfig), swaggerConfig);
    app.UseSwagger(o => { o.RouteTemplate = swaggerConfig.JsonRoute; });
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint(swaggerConfig.UiEndpoint, swaggerConfig.Name);
        o.OAuthAppName(swaggerConfig.Name);
        o.OAuthUsePkce();
    });
}
else
{
    if (!isOpenApiExport)
    {
        await app.InitializeDatabaseAsync();
    }
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (!context.Request.IsHttps) { context.Response.StatusCode = StatusCodes.Status426UpgradeRequired; return; }
        await next();
    });
}
app.UseRouting();

app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});
app.UseCors("default");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }

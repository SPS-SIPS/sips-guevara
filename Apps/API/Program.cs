using System.Security.Cryptography.X509Certificates;
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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors("default");

app.MapControllers();

app.Run();

using SPSS.API.Installers;
using SPSS.API.Jobs;
using Serilog;
using static SPSS.Domain.Extensions.InitializerExtensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddHttpContextAccessor();
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();
});

builder.Services.AddHttpClient();
builder.Services.InstallerServicesInAssembly(builder.Configuration);
builder.Services.AddHostedService<InvalidateExpireTokens>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.InitializeDatabaseAsync();
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
    await app.InitializeDatabaseAsync();
    app.UseHsts();
}

app.UseRouting();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors("default");

app.MapControllers();

app.Run();


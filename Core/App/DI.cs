using Microsoft.Extensions.DependencyInjection;
using SPSS.App.Common;
namespace SPSS.App;

public static class DI
{
    public static IServiceCollection AddApp(this IServiceCollection services, string? mapperLicenseKey = null)
    {
        services.AddSingleton<SPSS.App.Features.Auth.LoginAttemptLimiter>();
        services.AddMediator(o =>
        {
            o.ServiceLifetime = ServiceLifetime.Scoped;
        });
        services.AddAutoMapper(c =>
        {
            c.LicenseKey = mapperLicenseKey;
            c.AddProfile<Mappings>();
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));

        return services;
    }
}

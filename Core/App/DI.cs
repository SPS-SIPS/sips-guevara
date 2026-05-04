using Microsoft.Extensions.DependencyInjection;
using SPSS.App.Common;
using SPSS.App.Features.Balance.Services;
namespace SPSS.App;

public static class DI
{
    public static IServiceCollection AddApp(this IServiceCollection services)
    {
        services.AddMediator(o =>
        {
            o.ServiceLifetime = ServiceLifetime.Scoped;
        });
        services.AddAutoMapper(c =>
        {
            c.AddProfile<Mappings>();
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddScoped<IFailedTransactionComplianceClassificationRefresher, FailedTransactionComplianceClassificationRefresher>();

        return services;
    }
}

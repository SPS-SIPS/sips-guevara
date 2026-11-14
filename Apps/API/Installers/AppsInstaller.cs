using SPSS.App;
using Infrastructure;

namespace SPSS.API.Installers;

public class AppsInstaller : IInstaller
{
    public void InstallService(IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = new Action<ProblemDetailsContext>(context =>
            {
                var traceId = context.HttpContext.TraceIdentifier;
                var problemDetails = context.ProblemDetails;
            });
        });
        services.AddInfrastructure(configuration);
        services.AddApp(configuration["AutoMapper:LicenseKey"]);
    }
}
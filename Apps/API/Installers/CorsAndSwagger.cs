namespace SPSS.API.Installers;
public class CorsAndSwagger : IInstaller
{
    public void InstallService(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(o =>
         {
             // get configuration SwaggerConfig:Name
             o.SwaggerDoc("v1", new() { Title = configuration["SwaggerConfig:Name"], Version = "v1" });
             o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
             {
                 Description = "Please provide a JWT Token To get Authorized",
                 Name = "Authorization",
                 Type = SecuritySchemeType.Http,
                 BearerFormat = "JWT",
                 In = ParameterLocation.Header,
                 Scheme = "Bearer",
             });
             o.AddSecurityRequirement(new OpenApiSecurityRequirement
             {
                {
                    new OpenApiSecurityScheme
                        {
                            Name = "Bearer",
                            Type = SecuritySchemeType.Http,
                            In = ParameterLocation.Header,
                            BearerFormat = "JWT",
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                        },
                        Array.Empty<string>()
                }
             });
             o.OperationFilter<AuthorizeCheckOperationFilter>();
         });
        services.AddControllers(options =>
        {
            options.Filters.Add<ApiExceptionFilterAttribute>();
            options.Filters.Add<ResponseActionFilter>();
        }
        );

        // cors
        var corsPolicy = new CorsPolicies();
        configuration.Bind(nameof(CorsPolicies), corsPolicy);


        services.AddCors(options =>
         {
             options.AddPolicy("default",
             builder =>
             {
                 if (corsPolicy.Origins is { Length: > 0 })
                     builder.WithOrigins(corsPolicy.Origins).AllowAnyMethod().AllowAnyHeader();
             });
         });
    }
}
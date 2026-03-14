using Asp.Versioning;
using FluentValidation;
using RiskScreening.API.Shared.Domain.Repositories;
using RiskScreening.API.Shared.Infrastructure.Configuration;
using RiskScreening.API.Shared.Infrastructure.Persistence.Repositories;
using RiskScreening.API.Shared.Infrastructure.Pipeline;
using RiskScreening.API.Shared.Infrastructure.Web.ExceptionHandlers;
using Serilog;

namespace RiskScreening.API.Shared.Infrastructure.Extensions;

/// <summary>
///     Provides extension methods for <see cref="WebApplicationBuilder"/> to register
///     shared infrastructure services into the dependency injection container.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    ///     Configures Serilog as the application logger.
    ///     Reads minimum levels from <c>Serilog</c> section in configuration.
    ///     Enriches every log entry with <c>LogContext</c> properties (e.g. <c>CorrelationId</c>)
    ///     and a static <c>Application</c> label for Loki filtering.
    /// </summary>
    public static void AddLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, config) =>
            config.ReadFrom.Configuration(ctx.Configuration)
                  .Enrich.FromLogContext()
                  .Enrich.WithProperty("Application", "RiskScreening.API")
                  .WriteTo.Console());
    }

    /// <summary>
    ///     Registers all shared infrastructure services required by the application.
    ///     Includes the Unit of Work and any other cross-cutting infrastructure dependencies.
    /// </summary>
    public static void AddSharedInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Required for AppDbContext to read the current authenticated user from JWT claims
        // and stamp CreatedBy / UpdatedBy on every writing.
        builder.Services.AddHttpContextAccessor();

        // Required by UseExceptionHandler() when using IExceptionHandler implementations
        builder.Services.AddProblemDetails();

        // Exception handlers — registered in priority order (specific → general)
        // IExceptionHandler chain: ValidationExceptionHandler → DomainExceptionHandler → GlobalExceptionHandler
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<DomainExceptionHandler>();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    }

    /// <summary>
    ///     Registers API versioning using a header-based strategy via the <c>Api-Version</c> header.
    ///     Default version is <c>1.0</c> — clients that omit the header receive V1.
    /// </summary>
    public static void AddApiVersioning(this WebApplicationBuilder builder)
    {
        builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new HeaderApiVersionReader(ApiVersioning.Header);
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
    }

    /// <summary>
    ///     Registers the CORS policy named <c>AllowAllPolicy</c>.
    ///     Allowed origins are read from <c>Cors:AllowedOrigins</c> in configuration.
    ///     Falls back to allowing any origin if no origins are configured (development only).
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The same <see cref="WebApplicationBuilder"/> instance to allow method chaining.</returns>
    public static void AddCorsPolicy(this WebApplicationBuilder builder)
    {
        var settings = builder.Configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAllPolicy", policy =>
            {
                if (settings.AllowedOrigins.Length > 0)
                    policy.WithOrigins(settings.AllowedOrigins).AllowAnyMethod().AllowAnyHeader();
                else
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });
    }

    /// <summary>
    ///     Registers MediatR with all handlers found in the given assemblies
    ///     and adds the shared pipeline behaviors in order:
    ///     <list type="number">
    ///         <item><see cref="LoggingPipelineBehavior{TRequest,TResponse}"/> — logs execution and elapsed time.</item>
    ///         <item><see cref="ValidationPipelineBehavior{TRequest,TResponse}"/> — validates requests via FluentValidation before the handler runs.</item>
    ///     </list>
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <param name="assemblies">
    ///     The assemblies to scan for MediatR handlers and FluentValidation validators.
    /// </param>
    /// <returns>The same <see cref="WebApplicationBuilder"/> instance to allow method chaining.</returns>
    public static void AddMediator(this WebApplicationBuilder builder, params Type[] assemblies)
    {
        var assemblyArray = assemblies.Select(t => t.Assembly).ToArray();

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblyArray);
            cfg.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        builder.Services.AddValidatorsFromAssemblies(assemblyArray);
    }
}
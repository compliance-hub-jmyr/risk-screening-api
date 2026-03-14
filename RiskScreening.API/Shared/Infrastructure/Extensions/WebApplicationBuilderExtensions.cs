using Asp.Versioning;
using FluentValidation;
using RiskScreening.API.Shared.Domain.Repositories;
using RiskScreening.API.Shared.Infrastructure.Configuration;
using RiskScreening.API.Shared.Infrastructure.Persistence.Repositories;
using RiskScreening.API.Shared.Infrastructure.Pipeline;
using RiskScreening.API.Shared.Infrastructure.Web.ExceptionHandlers;

namespace RiskScreening.API.Shared.Infrastructure.Extensions;

/// <summary>
///     Provides extension methods for <see cref="WebApplicationBuilder"/> to register
///     shared infrastructure services into the dependency injection container.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    ///     Registers all shared infrastructure services required by the application.
    ///     Includes the Unit of Work and any other cross-cutting infrastructure dependencies.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The same <see cref="WebApplicationBuilder"/> instance to allow method chaining.</returns>
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
using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Modules.IAM.Infrastructure.Extensions;
using RiskScreening.API.Modules.Scraping.Infrastructure.Extensions;
using RiskScreening.API.Modules.Suppliers.Infrastructure.Extensions;
using RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Extensions;
using RiskScreening.API.Shared.Infrastructure.Extensions;
using RiskScreening.API.Shared.Infrastructure.Interfaces;
using RiskScreening.API.Shared.Infrastructure.Persistence;
using RiskScreening.API.Shared.Infrastructure.Persistence.Migrations;
using RiskScreening.API.Shared.Infrastructure.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// SERVICES
// ==========================================

// Serilog — replaces default ILogger with structured logging + LogContext enrichment
builder.AddLogging();

// OpenAPI documentation (native + Swashbuckle) with JWT auth and custom response filters
builder.AddOpenApiDocumentation();

// CORS — AllowAllPolicy (restrict origins in production)
builder.AddCorsPolicy();

// Registers MVC controllers for API routing
// KebabCaseRouteNamingConvention converts [controller] routes to a kebab-case automatically
// e.g., RiskScreeningsController → /api/risk-screenings
builder.Services.AddControllers(options =>
    options.Conventions.Add(new KebabCaseRouteNamingConvention()));

// API Versioning — header-based strategy via 'Api-Version' header
// Default version: 1.0 (clients without header receive V1)
builder.AddApiVersioning();

// MediatR + FluentValidation + pipeline behaviors (Logging → Validation → Handler)
builder.AddMediator(typeof(Program));

// Shared infrastructure: IUnitOfWork and cross-cutting services
builder.AddSharedInfrastructure();

// IP rate limiting — tiered: sign-in 5/min, lists 20/min, general 100/min
builder.AddRateLimiting();

// IAM module: repositories, BCrypt, JWT auth, seeder
builder.AddIamModule();

// Scraping module: HTTP clients, IMemoryCache
builder.AddScrapingModule();

// Suppliers module: repositories, EF configurations
builder.AddSuppliersModule();

// Entity Framework Core with SQL Server
// Reads connection string from app settings.{Environment}.json
// or from environment variables (ConnectionStrings__DefaultConnection)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================
// PIPELINE
// ==========================================

var app = builder.Build();

// ==========================================
// MIGRATIONS
// ==========================================

// Runs pending SQL scripts from Migrations/Scripts/*.sql (embedded resources).
DatabaseMigrator.Migrate(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Seed IAM data — system roles + default admin user
await app.UseIamModuleAsync();

if (app.Environment.IsDevelopment())
    // Native OpenAPI JSON: /openapi/v1.json
    // Swashbuckle JSON:    /swagger/v1/swagger.json
    // Swagger UI:          /swagger
    app.UseOpenApiDocumentation();

// Applies the AllowAllPolicy registered by AddCorsPolicy()
app.UseCorsPolicy();

// Rewrites rate-limit 429 responses to standard ErrorResponse JSON format
app.UseMiddleware<RateLimitResponseMiddleware>();

// IP rate limiting — must run before auth (tiered: sign-in 5/min, lists 20/min, general 100/min)
app.UseRateLimiting();

// Assigns X-Correlation-ID to every request and pushes it into Serilog LogContext
app.UseMiddleware<CorrelationIdMiddleware>();

// JWT Bearer authentication + role-based authorization
app.UseAuthentication();
app.UseAuthorization();

// Global exception handling — maps all exceptions to ErrorResponse JSON
// Chain: ValidationExceptionHandler → DomainExceptionHandler → GlobalExceptionHandler
app.UseExceptionHandler();

// Redirects HTTP requests to HTTPS
app.UseHttpsRedirection();

// Maps [Route], [HttpGet], [HttpPost], etc. on all controllers to HTTP endpoints
app.MapControllers();

app.Run();
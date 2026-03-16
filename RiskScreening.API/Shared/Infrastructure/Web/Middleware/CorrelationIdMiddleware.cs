using System.Diagnostics;
using Serilog.Context;

namespace RiskScreening.API.Shared.Infrastructure.Web.Middleware;

/// <summary>
///     Middleware that assigns a <c>CorrelationId</c> to every incoming request and pushes it
///     into the Serilog <see cref="LogContext"/> so all log entries within the request
///     automatically carry the property — no manual threading required.
/// </summary>
/// <remarks>
///     The <c>CorrelationId</c> is resolved in priority order:
///     <list type="number">
///         <item><description>Incoming <c>X-Correlation-ID</c> header (forwarded from upstream service or client).</description></item>
///         <item><description><see cref="Activity.Current"/> TraceId (OpenTelemetry / distributed tracing).</description></item>
///         <item><description>A new <see cref="Guid"/> generated for this request.</description></item>
///     </list>
///     The resolved value is always echoed back in the <c>X-Correlation-ID</c> response header
///     so clients can include it in bug reports.
/// </remarks>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Activity.Current?.TraceId.ToString()
                            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
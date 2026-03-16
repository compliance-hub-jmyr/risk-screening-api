using System.Text.Json;
using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Interfaces.REST.Resources;

namespace RiskScreening.API.Shared.Infrastructure.Web.Middleware;

/// <summary>
///     Intercepts HTTP 429 responses produced by <c>AspNetCoreRateLimit</c>
///     and rewrites the body to the standard <see cref="ErrorResponse"/> format.
///     Wraps the response stream only for rate-limited requests.
///     Must be registered <b>before</b> <c>UseRateLimiting()</c> in the pipeline.
/// </summary>
public sealed class RateLimitResponseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await next(context);

        if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
        {
            var retryAfter = context.Response.Headers.RetryAfter.FirstOrDefault();

            var error = new ErrorResponse
            {
                Title = "Rate limit exceeded",
                Status = StatusCodes.Status429TooManyRequests,
                Instance = context.Request.Path,
                ErrorNumber = ErrorCodes.RateLimitExceeded,
                ErrorCode = ErrorCodes.RateLimitExceededCode,
                Message = retryAfter is not null
                    ? $"Too many requests. Please retry after {retryAfter} seconds."
                    : "Too many requests. Please slow down."
            };

            buffer.SetLength(0);
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.SerializeToUtf8Bytes(error);
            context.Response.ContentLength = json.Length;
            await buffer.WriteAsync(json);
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }
}
using Microsoft.AspNetCore.Diagnostics;
using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Interfaces.REST.Resources;
using RiskScreening.API.Shared.Infrastructure.Exceptions;

namespace RiskScreening.API.Shared.Infrastructure.Web.ExceptionHandlers;

/// <summary>
///     Fallback handler for all unhandled exceptions.
///     Registered last — only triggered when no previous handler returned <c>true</c>.
///     Always returns HTTP 500 Internal Server Error.
/// </summary>
/// <remarks>
///     In production: returns a generic message without exposing internal details.
///     In development: it includes the exception message for easier debugging.
/// </remarks>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IWebHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, errorNumber, errorCode) = exception switch
        {
            InfrastructureException infra => (
                StatusCodes.Status500InternalServerError,
                infra.ErrorNumber,
                infra.ErrorCode),
            _ => (
                StatusCodes.Status500InternalServerError,
                ErrorCodes.InternalServerError,
                ErrorCodes.InternalServerErrorCode)
        };

        logger.LogError(
            exception,
            "Unhandled exception [{ErrorCode}] on {Path}: {Message}",
            errorCode,
            httpContext.Request.Path,
            exception.Message);

        var message = env.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred. Please try again later.";

        var error = new ErrorResponse
        {
            Title = "An unexpected error occurred",
            Status = statusCode,
            Instance = httpContext.Request.Path,
            ErrorNumber = errorNumber,
            ErrorCode = errorCode,
            Message = message
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}

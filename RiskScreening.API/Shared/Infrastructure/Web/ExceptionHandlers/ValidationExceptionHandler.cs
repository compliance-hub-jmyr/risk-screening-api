using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Interfaces.REST.Resources;

namespace RiskScreening.API.Shared.Infrastructure.Web.ExceptionHandlers;

/// <summary>
///     Handles <see cref="FluentValidation.ValidationException"/> thrown by
///     <c>ValidationPipelineBehavior</c> and maps it to a 400 Bad Request with
///     per-field errors.
/// </summary>
public sealed class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        logger.LogWarning(
            "Validation failed on {Path}: {Count} error(s)",
            httpContext.Request.Path,
            validationException.Errors.Count());

        var fieldErrors = validationException.Errors
            .Select(e => new ErrorResponse.FieldError(e.PropertyName, e.ErrorMessage, e.AttemptedValue))
            .ToList();

        var error = new ErrorResponse
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Instance = httpContext.Request.Path,
            ErrorNumber = ErrorCodes.ValidationFailed,
            ErrorCode = ErrorCodes.ValidationFailedCode,
            Message = "Request validation failed. Check field errors for details.",
            FieldErrors = fieldErrors
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
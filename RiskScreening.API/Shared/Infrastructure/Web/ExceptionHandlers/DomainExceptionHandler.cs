using Microsoft.AspNetCore.Diagnostics;
using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Interfaces.REST.Resources;

namespace RiskScreening.API.Shared.Infrastructure.Web.ExceptionHandlers;

/// <summary>
///     Handles all <see cref="DomainException"/> subtypes and maps them to the
///     appropriate HTTP status code + <see cref="ErrorResponse"/> body.
/// </summary>
/// <remarks>
///     Registered first so domain exceptions are caught before the global fallback.
///     Mapping:
///     <list type="table">
///         <item><term><see cref="EntityNotFoundException"/></term><description>404 Not Found</description></item>
///         <item><term><see cref="BusinessRuleViolationException"/></term><description>409 Conflict</description></item>
///         <item><term><see cref="AuthenticationException"/></term><description>401 Unauthorized</description></item>
///         <item><term><see cref="AuthorizationException"/></term><description>403 Forbidden</description></item>
///         <item><term><see cref="InvalidValueException"/></term><description>400 Bad Request</description></item>
///         <item><term><see cref="DomainException"/> (other)</term><description>400 Bad Request</description></item>
///     </list>
/// </remarks>
public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
            return false;

        var statusCode = GetStatusCode(domainException);

        logger.Log(
            statusCode >= 500 ? LogLevel.Error : LogLevel.Warning,
            exception,
            "Domain exception [{ErrorCode}]: {Message}",
            domainException.ErrorCode,
            domainException.Message);

        // Map DomainValidationException field errors to the response if present
        IReadOnlyList<ErrorResponse.FieldError>? fieldErrors = null;
        if (domainException is DomainValidationException domainValidation)
        {
            fieldErrors = domainValidation.FieldErrors
                .Select(e => new ErrorResponse.FieldError(e.Field, e.Message, e.RejectedValue))
                .ToList();
        }

        var error = new ErrorResponse
        {
            Title = GetTitle(domainException),
            Status = statusCode,
            Instance = httpContext.Request.Path,
            ErrorNumber = domainException.ErrorNumber,
            ErrorCode = domainException.ErrorCode,
            Message = domainException.Message,
            FieldErrors = fieldErrors
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }

    private static int GetStatusCode(DomainException exception) => exception switch
    {
        EntityNotFoundException => StatusCodes.Status404NotFound,
        BusinessRuleViolationException => StatusCodes.Status409Conflict,
        AuthenticationException => StatusCodes.Status401Unauthorized,
        AuthorizationException => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };

    private static string GetTitle(DomainException exception) => exception switch
    {
        EntityNotFoundException => "Resource not found",
        BusinessRuleViolationException => "Business rule violation",
        AuthenticationException => "Unauthorized",
        AuthorizationException => "Forbidden",
        _ => "Bad request"
    };
}

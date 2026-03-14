using FluentValidation;
using MediatR;

namespace RiskScreening.API.Shared.Infrastructure.Pipeline;

/// <summary>
///     MediatR pipeline behavior that validates every incoming request using FluentValidation.
///     If one or more validators are registered for the request type, they are all executed
///     before the handler is invoked. If validation fails, a <see cref="ValidationException"/>
///     is thrown and the handler is never called.
/// </summary>
/// <remarks>
///     Handlers are guaranteed to receive only valid requests.
///     Register validators by implementing <c>AbstractValidator&lt;TRequest&gt;</c>.
///     If no validator is registered for a request, it passes through without error.
/// </remarks>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
public class ValidationPipelineBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    ///     Runs all registered validators for <typeparamref name="TRequest"/>.
    ///     Throws <see cref="ValidationException"/> if any validation rule fails.
    /// </summary>
    /// <param name="request">The incoming request to validate.</param>
    /// <param name="next">The delegate representing the next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response produced by the handler if validation passes.</returns>
    /// <exception cref="ValidationException">
    ///     Thrown when one or more validation rules fail.
    /// </exception>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}

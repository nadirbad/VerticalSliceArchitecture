using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace VerticalSliceArchitecture.Application.Common;

/// <summary>
/// An endpoint filter that automatically validates requests using FluentValidation.
/// </summary>
/// <typeparam name="TRequest">The type of the request to validate.</typeparam>
public class ValidationFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    /// <summary>
    /// Invokes the validation filter logic.
    /// </summary>
    /// <param name="context">The endpoint filter invocation context.</param>
    /// <param name="next">The next filter or endpoint handler in the pipeline.</param>
    /// <returns>The result of the endpoint invocation or a validation problem if validation fails.</returns>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // Try to find the request object in the arguments
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        // If no request found, continue to next delegate
        if (request is null)
        {
            return await next(context);
        }

        // Try to resolve a validator for this request type
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();

        // If no validator registered, continue to next delegate
        if (validator is null)
        {
            return await next(context);
        }

        // Validate the request
        var validationResult = await validator.ValidateAsync(request);

        // If validation succeeds, continue to next delegate
        if (validationResult.IsValid)
        {
            return await next(context);
        }

        // Convert validation failures to ErrorOr errors and return Problem Details
        var errors = validationResult.Errors
            .Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage))
            .ToList();

        return MinimalApiProblemHelper.Problem(errors);
    }
}
using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace VerticalSliceArchitecture.Application.Common;

/// <summary>
/// Provides helper methods for converting ErrorOr results to Problem Details responses in Minimal APIs.
/// Replicates the error handling behavior of ApiControllerBase for use with Minimal API endpoints.
/// </summary>
public class MinimalApiProblemHelper
{
    /// <summary>
    /// Converts a list of errors to an appropriate Problem Details response.
    /// </summary>
    /// <param name="errors">The list of errors to convert.</param>
    /// <returns>An IResult representing the appropriate Problem Details response.</returns>
    public static IResult Problem(List<Error> errors)
    {
        if (errors.Count is 0)
        {
            return CreateProblemResult(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }

        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            return CreateValidationProblem(errors);
        }

        return CreateProblemForError(errors[0]);
    }

    private static ProblemHttpResult CreateProblemForError(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        return CreateProblemResult(statusCode, error.Description);
    }

    private static ValidationProblem CreateValidationProblem(List<Error> errors)
    {
        var problemDetails = new HttpValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        };

        var errorsDictionary = new Dictionary<string, string[]>();
        foreach (var error in errors)
        {
            if (errorsDictionary.TryGetValue(error.Code, out var existingErrorArray))
            {
                var existingErrors = existingErrorArray.ToList();
                existingErrors.Add(error.Description);
                errorsDictionary[error.Code] = existingErrors.ToArray();
            }
            else
            {
                errorsDictionary[error.Code] = new[] { error.Description };
            }
        }

        problemDetails.Errors = errorsDictionary;

        return TypedResults.ValidationProblem(
            errors: errorsDictionary,
            title: problemDetails.Title,
            type: problemDetails.Type);
    }

    private static ProblemHttpResult CreateProblemResult(int statusCode, string title)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = GetProblemType(statusCode),
        };

        return TypedResults.Problem(problemDetails);
    }

    private static string GetProblemType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    };
}
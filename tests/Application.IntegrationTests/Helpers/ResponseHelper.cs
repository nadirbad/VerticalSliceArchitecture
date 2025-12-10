using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for parsing and asserting HTTP responses in integration tests.
/// </summary>
public static class ResponseHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Deserializes ProblemDetails from an error response.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <returns>The deserialized ProblemDetails or null.</returns>
    public static async Task<ProblemDetails?> GetProblemDetailsAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
    }

    /// <summary>
    /// Gets validation errors from ProblemDetails response.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <returns>Dictionary of property names to error messages.</returns>
    public static Dictionary<string, string[]>? GetValidationErrors(ProblemDetails? problemDetails)
    {
        if (problemDetails?.Extensions == null || !problemDetails.Extensions.TryGetValue("errors", out var errorsObj))
        {
            return null;
        }

        if (errorsObj is not JsonElement errorsElement)
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, string[]>>(
            errorsElement.GetRawText(),
            JsonOptions);
    }

    /// <summary>
    /// Checks if a validation error exists for a specific property.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <param name="propertyName">The property name to check.</param>
    /// <returns>True if the property has validation errors.</returns>
    public static bool HasValidationError(ProblemDetails? problemDetails, string propertyName)
    {
        var errors = GetValidationErrors(problemDetails);
        return errors?.ContainsKey(propertyName) ?? false;
    }

    /// <summary>
    /// Gets the first validation error message for a property.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The first error message or null.</returns>
    public static string? GetFirstValidationError(ProblemDetails? problemDetails, string propertyName)
    {
        var errors = GetValidationErrors(problemDetails);
        return errors?.TryGetValue(propertyName, out var messages) == true && messages.Length > 0
            ? messages[0]
            : null;
    }

    /// <summary>
    /// Gets all validation error messages for a specific property.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>Array of error messages or empty array if none found.</returns>
    public static string[] GetAllValidationErrors(ProblemDetails? problemDetails, string propertyName)
    {
        var errors = GetValidationErrors(problemDetails);
        return errors?.TryGetValue(propertyName, out var messages) == true
            ? messages
            : Array.Empty<string>();
    }

    /// <summary>
    /// Gets the error code from ProblemDetails (typically from the type field or custom extensions).
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <returns>The error code or null.</returns>
    public static string? GetErrorCode(ProblemDetails? problemDetails)
    {
        if (problemDetails?.Extensions == null)
        {
            return null;
        }

        // Try to get code from extensions
        if (problemDetails.Extensions.TryGetValue("code", out var codeObj))
        {
            return codeObj?.ToString();
        }

        // Try to extract from type field (common pattern: "https://tools.ietf.org/html/rfc7231#section-6.5.1" or custom codes)
        if (!string.IsNullOrEmpty(problemDetails.Type) && problemDetails.Type.Contains('/'))
        {
            var parts = problemDetails.Type.Split('/');
            return parts[^1]; // Return last segment
        }

        return problemDetails.Type;
    }

    /// <summary>
    /// Gets custom error details from ProblemDetails extensions.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <param name="key">The extension key to retrieve.</param>
    /// <returns>The extension value or null.</returns>
    public static object? GetExtension(ProblemDetails? problemDetails, string key)
    {
        return problemDetails?.Extensions?.TryGetValue(key, out var value) == true ? value : null;
    }

    /// <summary>
    /// Checks if ProblemDetails represents a validation error (status 400).
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <returns>True if this is a validation error.</returns>
    public static bool IsValidationError(ProblemDetails? problemDetails)
    {
        return problemDetails?.Status == 400;
    }

    /// <summary>
    /// Checks if ProblemDetails represents a not found error (status 404).
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <returns>True if this is a not found error.</returns>
    public static bool IsNotFoundError(ProblemDetails? problemDetails)
    {
        return problemDetails?.Status == 404;
    }

    /// <summary>
    /// Checks if ProblemDetails represents a conflict error (status 409).
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <returns>True if this is a conflict error.</returns>
    public static bool IsConflictError(ProblemDetails? problemDetails)
    {
        return problemDetails?.Status == 409;
    }

    /// <summary>
    /// Checks if ProblemDetails represents an unprocessable entity error (status 422).
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <returns>True if this is an unprocessable entity error.</returns>
    public static bool IsUnprocessableEntityError(ProblemDetails? problemDetails)
    {
        return problemDetails?.Status == 422;
    }

    /// <summary>
    /// Gets the total count of validation errors across all properties.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <returns>The total number of validation errors.</returns>
    public static int GetValidationErrorCount(ProblemDetails? problemDetails)
    {
        var errors = GetValidationErrors(problemDetails);
        return errors?.Values.Sum(messages => messages.Length) ?? 0;
    }

    /// <summary>
    /// Gets all property names that have validation errors.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <returns>Collection of property names with validation errors.</returns>
    public static IEnumerable<string> GetValidationErrorPropertyNames(ProblemDetails? problemDetails)
    {
        var errors = GetValidationErrors(problemDetails);
        return errors?.Keys ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// Checks if the ProblemDetails contains any validation errors.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails instance.</param>
    /// <returns>True if validation errors exist.</returns>
    public static bool HasAnyValidationErrors(ProblemDetails? problemDetails)
    {
        return GetValidationErrorCount(problemDetails) > 0;
    }
}
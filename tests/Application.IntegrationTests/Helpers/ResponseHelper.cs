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
}

using System.Net.Http.Json;
using System.Text.Json;

namespace VerticalSliceArchitecture.Application.IntegrationTests.Helpers;

/// <summary>
/// Extension methods for HttpClient to simplify integration testing.
/// </summary>
public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Sends a POST request and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request content.</typeparam>
    /// <typeparam name="TResponse">The type of the response content.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="content">The request content.</param>
    /// <returns>The deserialized response.</returns>
    public static async Task<TResponse?> PostAndGetResponseAsync<TRequest, TResponse>(
        this HttpClient client,
        string requestUri,
        TRequest content)
    {
        var response = await client.PostAsJsonAsync(requestUri, content);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    /// <summary>
    /// Sends a PUT request and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request content.</typeparam>
    /// <typeparam name="TResponse">The type of the response content.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="content">The request content.</param>
    /// <returns>The deserialized response.</returns>
    public static async Task<TResponse?> PutAndGetResponseAsync<TRequest, TResponse>(
        this HttpClient client,
        string requestUri,
        TRequest content)
    {
        var response = await client.PutAsJsonAsync(requestUri, content);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    /// <summary>
    /// Sends a GET request and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response content.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>The deserialized response.</returns>
    public static async Task<TResponse?> GetAndDeserializeAsync<TResponse>(
        this HttpClient client,
        string requestUri)
    {
        var response = await client.GetAsync(requestUri);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }
}

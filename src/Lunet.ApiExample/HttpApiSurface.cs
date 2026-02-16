namespace Lunet.ApiExample.Http;

/// <summary>
/// Available HTTP transports for the sample API.
/// </summary>
public enum HttpTransport
{
    /// <summary>
    /// HTTP/1.1 transport.
    /// </summary>
    Http1,

    /// <summary>
    /// HTTP/2 transport.
    /// </summary>
    Http2
}

/// <summary>
/// Represents an HTTP request.
/// </summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The request path.</param>
public sealed record class HttpRequest(string Method, string Path);

/// <summary>
/// Represents retry settings for HTTP calls.
/// </summary>
/// <param name="MaxAttempts">The maximum number of attempts.</param>
/// <param name="BackoffMilliseconds">The retry backoff in milliseconds.</param>
public readonly record struct RetrySettings(int MaxAttempts, int BackoffMilliseconds);

/// <summary>
/// Represents endpoint timeout values.
/// </summary>
public struct EndpointTimeout
{
    /// <summary>
    /// Gets or sets the connect timeout in milliseconds.
    /// </summary>
    public int ConnectTimeoutMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the request timeout in milliseconds.
    /// </summary>
    public int RequestTimeoutMilliseconds { get; set; }
}

/// <summary>
/// Represents a typed HTTP endpoint.
/// </summary>
public interface IHttpEndpoint
{
    /// <summary>
    /// Gets the route template.
    /// </summary>
    string Route { get; }

    /// <summary>
    /// Invokes the endpoint handler.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <returns>A response body.</returns>
    string Invoke(HttpRequest request);
}

/// <summary>
/// Default implementation of <see cref="IHttpEndpoint"/>.
/// </summary>
public sealed class HttpEndpoint : IHttpEndpoint
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpEndpoint"/>.
    /// </summary>
    /// <param name="route">The route template.</param>
    /// <param name="transport">The transport protocol.</param>
    public HttpEndpoint(string route, HttpTransport transport)
    {
        Route = route;
        Transport = transport;
    }

    /// <summary>
    /// Gets the transport protocol.
    /// </summary>
    public HttpTransport Transport { get; }

    /// <summary>
    /// Gets the route template.
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// Gets or sets endpoint timeout configuration.
    /// </summary>
    public EndpointTimeout Timeout { get; set; }

    /// <summary>
    /// Invokes the endpoint handler.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <returns>A response body.</returns>
    public string Invoke(HttpRequest request) => $"{request.Method} {request.Path} via {Transport}";
}

/// <summary>
/// Extension methods for HTTP sample models.
/// </summary>
public static class HttpEndpointExtensions
{
    /// <summary>
    /// Returns a display label for the endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint instance.</param>
    /// <returns>A display label.</returns>
    public static string ToDisplayLabel(this IHttpEndpoint endpoint) => endpoint.Route;
}

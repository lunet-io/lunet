using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunet.ApiExample;

/// <summary>
/// Formats a full display name.
/// </summary>
/// <param name="firstName">The first name.</param>
/// <param name="lastName">The last name.</param>
/// <returns>The formatted display name.</returns>
public delegate string NameFormatter(string? firstName, string? lastName);

/// <summary>
/// Severity levels emitted by the sample API.
/// </summary>
public enum Severity
{
    /// <summary>
    /// Trace level.
    /// </summary>
    Trace,

    /// <summary>
    /// Informational level.
    /// </summary>
    Information,

    /// <summary>
    /// Warning level.
    /// </summary>
    Warning,

    /// <summary>
    /// Error level.
    /// </summary>
    Error
}

/// <summary>
/// Represents a paged API response.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The returned items.</param>
/// <param name="TotalCount">The total count.</param>
public readonly record struct PageResult<T>(IReadOnlyList<T> Items, int TotalCount);

/// <summary>
/// A user model exposed by the API.
/// </summary>
/// <param name="Id">The unique identifier.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The last name.</param>
public sealed record class ApiUser(string Id, string? FirstName, string? LastName);

/// <summary>
/// Basic repository contract used in examples.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T>
{
    /// <summary>
    /// Finds an entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The entity or <see langword="null"/>.</returns>
    T? FindById(string id);

    /// <summary>
    /// Returns all entities.
    /// </summary>
    /// <returns>The entity sequence.</returns>
    IEnumerable<T> FindAll();
}

/// <summary>
/// Represents a semantic API version.
/// </summary>
/// <param name="Major">The major version.</param>
/// <param name="Minor">The minor version.</param>
public readonly struct ApiVersion(int major, int minor) : IComparable<ApiVersion>, IEquatable<ApiVersion>
{
    /// <summary>
    /// Gets the major version.
    /// </summary>
    public int Major { get; } = major;

    /// <summary>
    /// Gets the minor version.
    /// </summary>
    public int Minor { get; } = minor;

    /// <summary>
    /// Compares two versions.
    /// </summary>
    /// <param name="other">The other version.</param>
    /// <returns>A comparison result.</returns>
    public int CompareTo(ApiVersion other) => Major != other.Major ? Major.CompareTo(other.Major) : Minor.CompareTo(other.Minor);

    /// <summary>
    /// Returns whether two versions are equal.
    /// </summary>
    /// <param name="other">The other version.</param>
    /// <returns><see langword="true"/> when both values are equal.</returns>
    public bool Equals(ApiVersion other) => Major == other.Major && Minor == other.Minor;

    /// <summary>
    /// Returns a textual representation.
    /// </summary>
    /// <returns>The version string.</returns>
    public override string ToString() => $"{Major}.{Minor}";

    /// <summary>
    /// Adds two versions.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The resulting version.</returns>
    public static ApiVersion operator +(ApiVersion left, ApiVersion right) => new(left.Major + right.Major, left.Minor + right.Minor);

    /// <summary>
    /// Checks whether the left version is greater than the right version.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when left is greater.</returns>
    public static bool operator >(ApiVersion left, ApiVersion right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Checks whether the left version is less than the right version.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when left is lower.</returns>
    public static bool operator <(ApiVersion left, ApiVersion right) => left.CompareTo(right) < 0;
}

/// <summary>
/// Defines a static factory contract.
/// </summary>
/// <typeparam name="TSelf">The self type.</typeparam>
public interface IStaticFactory<TSelf>
    where TSelf : IStaticFactory<TSelf>
{
    /// <summary>
    /// Creates a default instance.
    /// </summary>
    /// <returns>The created instance.</returns>
    static abstract TSelf CreateDefault();
}

/// <summary>
/// Defines an interface with a default implementation.
/// </summary>
public interface IWithDefaultImplementation
{
    /// <summary>
    /// Gets a textual description.
    /// </summary>
    /// <returns>A description string.</returns>
    string Describe() => "Default implementation";
}

/// <summary>
/// Implements <see cref="IWithDefaultImplementation"/>.
/// </summary>
public sealed class DefaultImplementation : IWithDefaultImplementation
{
}

/// <summary>
/// Main service used by the API sample.
/// </summary>
public class ApiClient : IStaticFactory<ApiClient>
{
    private readonly List<string> _tags = [];

    /// <summary>
    /// Initializes a new instance of <see cref="ApiClient"/>.
    /// </summary>
    /// <param name="name">The service name.</param>
    public ApiClient(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Raised when the severity changes.
    /// </summary>
    public event EventHandler<Severity>? SeverityChanged;

    /// <summary>
    /// Gets the service name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the base address.
    /// </summary>
    public string? BaseAddress { get; set; }

    /// <summary>
    /// Gets the configured tags.
    /// </summary>
    public IReadOnlyList<string> Tags => _tags;

    /// <summary>
    /// Creates a default client.
    /// </summary>
    /// <returns>A default client instance.</returns>
    public static ApiClient CreateDefault() => new("default");

    /// <summary>
    /// Adds a tag if it is not already present.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    public void AddTag(string tag)
    {
        if (_tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        _tags.Add(tag);
    }

    /// <summary>
    /// Executes a handler for the specified request.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request value.</param>
    /// <param name="handler">The handler delegate.</param>
    /// <returns>The handler response.</returns>
    public TResponse Execute<TRequest, TResponse>(TRequest request, Func<TRequest, TResponse> handler)
        where TRequest : notnull
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        return handler(request);
    }

    /// <summary>
    /// Emits a severity notification.
    /// </summary>
    /// <param name="severity">The severity value.</param>
    protected virtual void OnSeverityChanged(Severity severity) => SeverityChanged?.Invoke(this, severity);
}

/// <summary>
/// Extension methods for API sample models.
/// </summary>
public static class ApiClientExtensions
{
    /// <summary>
    /// Returns whether a client has a specific tag.
    /// </summary>
    /// <param name="client">The target client.</param>
    /// <param name="tag">The tag to search.</param>
    /// <returns><see langword="true"/> if the tag exists.</returns>
    public static bool HasTag(this ApiClient client, string tag)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.Tags.Any(item => string.Equals(item, tag, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Formats a user display name.
    /// </summary>
    /// <param name="user">The user value.</param>
    /// <param name="formatter">The formatter delegate.</param>
    /// <returns>The formatted name.</returns>
    public static string ToDisplayName(this ApiUser user, NameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        return formatter(user.FirstName, user.LastName);
    }
}

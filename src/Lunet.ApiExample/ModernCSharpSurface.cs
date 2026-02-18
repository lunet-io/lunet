using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Lunet.ApiExample.Advanced;

/// <summary>
/// Represents a transformation step in a processing pipeline.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
/// <param name="input">The input value.</param>
/// <returns>The transformed value.</returns>
public delegate TOutput PipelineStep<in TInput, out TOutput>(TInput input);

/// <summary>
/// A modern record class with required and init-only members.
/// </summary>
/// <param name="Version">The semantic version string.</param>
public sealed record class BuildInfo(string Version)
{
    /// <summary>
    /// Gets or initializes the source commit.
    /// </summary>
    public required string Commit { get; init; }

    /// <summary>
    /// Gets or initializes build tags.
    /// </summary>
    public required IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// Represents a feature toggle entry.
/// </summary>
/// <param name="Name">The feature name.</param>
/// <param name="Enabled">Whether the feature is enabled.</param>
public readonly record struct FeatureFlag(string Name, bool Enabled);

/// <summary>
/// Demonstrates checked and unsigned right-shift operators.
/// </summary>
/// <param name="value">The wrapped integer value.</param>
public readonly struct CheckedInteger(int value)
{
    /// <summary>
    /// Gets the wrapped integer value.
    /// </summary>
    public int Value { get; } = value;

    /// <summary>
    /// Adds two values.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>The added value.</returns>
    public static CheckedInteger operator +(CheckedInteger left, CheckedInteger right) => new(left.Value + right.Value);

    /// <summary>
    /// Adds two values in checked context.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>The added value.</returns>
    public static CheckedInteger operator checked +(CheckedInteger left, CheckedInteger right) => new(checked(left.Value + right.Value));

    /// <summary>
    /// Applies an unsigned right shift.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="count">The shift count.</param>
    /// <returns>The shifted value.</returns>
    public static CheckedInteger operator >>>(CheckedInteger value, int count) => new(value.Value >>> count);
}

/// <summary>
/// Contract based on static abstract interface members.
/// </summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
public interface IStaticFactory<TSelf>
    where TSelf : IStaticFactory<TSelf>
{
    /// <summary>
    /// Gets an empty instance.
    /// </summary>
    static abstract TSelf Empty { get; }

    /// <summary>
    /// Creates an instance from a value.
    /// </summary>
    /// <param name="value">The source integer value.</param>
    /// <returns>The created instance.</returns>
    static abstract TSelf Create(int value);
}

/// <summary>
/// Contract using the <c>allows ref struct</c> generic constraint.
/// </summary>
/// <typeparam name="TBuffer">The ref struct buffer type.</typeparam>
public interface IBufferConsumer<TBuffer>
    where TBuffer : allows ref struct
{
    /// <summary>
    /// Consumes a buffer value.
    /// </summary>
    /// <param name="buffer">The buffer to consume.</param>
    void Consume(TBuffer buffer);
}

/// <summary>
/// Demonstrates default interface member implementations.
/// </summary>
public interface IWithDefaultDiagnostics
{
    /// <summary>
    /// Gets a default diagnostic label.
    /// </summary>
    /// <returns>A diagnostic label.</returns>
    string GetLabel() => "default";
}

/// <summary>
/// Demonstrates explicit interface implementation methods.
/// </summary>
public sealed class DiagnosticSource : IWithDefaultDiagnostics, IDisposable
{
    /// <summary>
    /// Releases resources.
    /// </summary>
    public void Dispose()
    {
    }

    string IWithDefaultDiagnostics.GetLabel() => "custom";
}

/// <summary>
/// Demonstrates primary constructors on classes.
/// </summary>
/// <param name="name">The client name.</param>
/// <param name="endpoint">The service endpoint.</param>
public sealed class PrimaryClient(string name, Uri endpoint)
{
    /// <summary>
    /// Gets the client name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the endpoint URI.
    /// </summary>
    public Uri Endpoint { get; } = endpoint;
}

/// <summary>
/// Demonstrates primary constructors on structs.
/// </summary>
/// <param name="retryCount">The retry count.</param>
/// <param name="timeout">The timeout value.</param>
public readonly struct RetryOptions(int retryCount, TimeSpan timeout)
{
    /// <summary>
    /// Gets the retry count.
    /// </summary>
    public int RetryCount { get; } = retryCount;

    /// <summary>
    /// Gets the timeout value.
    /// </summary>
    public TimeSpan Timeout { get; } = timeout;
}

/// <summary>
/// Demonstrates native integer and function pointer public API members.
/// </summary>
public unsafe class NativeInteropSurface
{
    /// <summary>
    /// Gets or sets the native handle value.
    /// </summary>
    public nint Handle { get; set; }

    /// <summary>
    /// Gets or sets a callback pointer.
    /// </summary>
    public delegate* unmanaged[Cdecl]<int, int> Callback { get; set; }
}

/// <summary>
/// Demonstrates <c>ref readonly</c> returns and <c>scoped</c> parameters.
/// </summary>
public class RefSemanticsSurface
{
    /// <summary>
    /// Returns a readonly reference to the provided value.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <returns>A readonly reference.</returns>
    public ref readonly int RefReadonlyReturn(ref readonly int value) => ref value;

    /// <summary>
    /// Parses a scoped span.
    /// </summary>
    /// <param name="value">The scoped input value.</param>
    /// <returns>The first character or <c>\0</c>.</returns>
    public char ParseFirst(scoped ReadOnlySpan<char> value) => value.IsEmpty ? '\0' : value[0];

    /// <summary>
    /// Sums values using a params collection.
    /// </summary>
    /// <param name="values">The values to sum.</param>
    /// <returns>The summed value.</returns>
    public int Sum(params ReadOnlySpan<int> values)
    {
        var total = 0;
        foreach (var item in values)
        {
            total += item;
        }
        return total;
    }
}

/// <summary>
/// Demonstrates extension members introduced in C# 14.
/// </summary>
public static class ApiUserExtensions
{
    extension(global::Lunet.ApiExample.ApiUser user)
    {
        /// <summary>
        /// Gets the normalized display name.
        /// </summary>
        public string DisplayName => string.Join(' ', new[] { user.FirstName, user.LastName }.Where(static x => !string.IsNullOrWhiteSpace(x)));

        /// <summary>
        /// Returns whether the user has at least one visible name part.
        /// </summary>
        /// <returns><see langword="true"/> when the name is not empty.</returns>
        public bool HasVisibleName() => !string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName);
    }
}

/// <summary>
/// Demonstrates ValueTuple members in public APIs.
/// </summary>
public static class TupleHelpers
{
    /// <summary>
    /// Creates a named tuple from a name and a value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">The name component.</param>
    /// <param name="value">The value component.</param>
    /// <returns>A named tuple <c>(Name, Value)</c>.</returns>
    public static (string Name, T Value) CreatePair<T>(string name, T value) => (name, value);

    /// <summary>
    /// Splits an RGB color value into its components.
    /// </summary>
    /// <param name="rgb">The packed RGB value (0xRRGGBB).</param>
    /// <returns>A tuple of <c>(R, G, B)</c> byte components.</returns>
    public static (byte R, byte G, byte B) SplitRgb(int rgb) =>
        ((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));

    /// <summary>
    /// Deconstructs a key/value pair into a plain tuple.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="pair">The key/value pair to convert.</param>
    /// <returns>A tuple of <c>(Key, Value)</c>.</returns>
    public static (TKey Key, TValue Value) AsTuple<TKey, TValue>(KeyValuePair<TKey, TValue> pair) =>
        (pair.Key, pair.Value);
}

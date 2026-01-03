using System;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Configuration options for Ripples state management.
/// </summary>
public sealed record RippleOptions
{
    /// <summary>
    ///     Gets the default options instance.
    /// </summary>
    public static RippleOptions Default { get; } = new();

    /// <summary>
    ///     Gets the batch size for prefetch operations.
    ///     Defaults to 20.
    /// </summary>
    public int BatchSize { get; init; } = 20;

    /// <summary>
    ///     Gets a value indicating whether batching is enabled for prefetch operations.
    ///     Defaults to true.
    /// </summary>
    public bool EnableBatching { get; init; } = true;

    /// <summary>
    ///     Gets the maximum number of warm tier entries.
    ///     Defaults to 100.
    /// </summary>
    public int MaxWarmEntries { get; init; } = 100;

    /// <summary>
    ///     Gets the warm tier timeout before eviction to cold.
    ///     Defaults to 30 seconds.
    /// </summary>
    public TimeSpan WarmTierTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
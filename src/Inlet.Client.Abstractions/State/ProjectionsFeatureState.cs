using System;
using System.Collections.Immutable;

using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Inlet.Client.Abstractions.State;

/// <summary>
///     Feature state containing all server-synced projection data.
/// </summary>
/// <remarks>
///     <para>
///         This state follows the Redux pattern: all projection data is stored in a single
///         feature state slice, updated only through dispatched actions and reducers.
///     </para>
///     <para>
///         Projections are keyed by a composite of projection type and entity ID.
///         Use <see cref="GetProjection{T}" /> and related methods for typed access.
///     </para>
/// </remarks>
public sealed record ProjectionsFeatureState : IFeatureState
{
    /// <summary>
    ///     Gets the unique key identifying this feature state in the store.
    /// </summary>
    public static string FeatureKey => "projections";

    /// <summary>
    ///     Gets the internal projection entries keyed by "{TypeFullName}:{EntityId}".
    /// </summary>
    /// <remarks>
    ///     Values are <see cref="ProjectionEntry{T}" /> instances boxed as objects.
    ///     Use the typed accessor methods for safe retrieval.
    /// </remarks>
    public ImmutableDictionary<string, object> Entries { get; init; } = ImmutableDictionary<string, object>.Empty;

    /// <summary>
    ///     Creates the composite key for a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The composite key.</returns>
    public static string GetKey<T>(
        string entityId
    )
        where T : class =>
        $"{typeof(T).FullName}:{entityId}";

    /// <summary>
    ///     Gets the projection entry for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The projection entry, or null if not found.</returns>
    public ProjectionEntry<T>? GetEntry<T>(
        string entityId
    )
        where T : class
    {
        string key = GetKey<T>(entityId);
        if (Entries.TryGetValue(key, out object? value) && value is ProjectionEntry<T> entry)
        {
            return entry;
        }

        return null;
    }

    /// <summary>
    ///     Gets the projection data for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The projection data, or null if not loaded.</returns>
    public T? GetProjection<T>(
        string entityId
    )
        where T : class =>
        GetEntry<T>(entityId)?.Data;

    /// <summary>
    ///     Gets the error for a projection, if any.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The error, or null if no error.</returns>
    public Exception? GetProjectionError<T>(
        string entityId
    )
        where T : class =>
        GetEntry<T>(entityId)?.Error;

    /// <summary>
    ///     Gets the version of a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The server version, or -1 if not loaded.</returns>
    public long GetProjectionVersion<T>(
        string entityId
    )
        where T : class =>
        GetEntry<T>(entityId)?.Version ?? -1;

    /// <summary>
    ///     Gets the connection state for a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>True if the projection is connected to the server.</returns>
    public bool IsProjectionConnected<T>(
        string entityId
    )
        where T : class =>
        GetEntry<T>(entityId)?.IsConnected ?? false;

    /// <summary>
    ///     Gets the loading state for a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>True if the projection is currently loading.</returns>
    public bool IsProjectionLoading<T>(
        string entityId
    )
        where T : class =>
        GetEntry<T>(entityId)?.IsLoading ?? false;

    /// <summary>
    ///     Returns a new state with the specified entry added or updated.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="entry">The entry to set.</param>
    /// <returns>A new state with the entry.</returns>
    public ProjectionsFeatureState WithEntry<T>(
        string entityId,
        ProjectionEntry<T> entry
    )
        where T : class
    {
        string key = GetKey<T>(entityId);
        return this with
        {
            Entries = Entries.SetItem(key, entry),
        };
    }

    /// <summary>
    ///     Returns a new state with the specified entry transformed.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="transform">The transformation function.</param>
    /// <returns>A new state with the transformed entry.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="entityId" /> or <paramref name="transform" /> is null.
    /// </exception>
    public ProjectionsFeatureState WithEntryTransform<T>(
        string entityId,
        Func<ProjectionEntry<T>, ProjectionEntry<T>> transform
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentNullException.ThrowIfNull(transform);
        ProjectionEntry<T> current = GetEntry<T>(entityId) ?? ProjectionEntry<T>.Empty;
        ProjectionEntry<T> updated = transform(current);
        return WithEntry(entityId, updated);
    }
}
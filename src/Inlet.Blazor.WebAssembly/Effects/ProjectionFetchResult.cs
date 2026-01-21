using System;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Represents the result of fetching a projection.
/// </summary>
/// <remarks>
///     <para>
///         This type distinguishes between three fetch outcomes:
///         <list type="bullet">
///             <item>
///                 <term>Success with data</term>
///                 <description>
///                     <see cref="Data" /> is populated with the projection,
///                     <see cref="IsNotFound" /> is <c>false</c>.
///                 </description>
///             </item>
///             <item>
///                 <term>Not found (no events yet)</term>
///                 <description>
///                     Use <see cref="NotFound" /> sentinel. The subscription remains valid
///                     and will receive updates when events arrive.
///                 </description>
///             </item>
///             <item>
///                 <term>Unregistered type</term>
///                 <description>
///                     The fetcher returns <c>null</c> to indicate the projection type
///                     is not supported.
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public sealed record ProjectionFetchResult
{
    /// <summary>
    ///     Gets the sentinel value indicating the projection was not found (HTTP 404).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is distinct from <c>null</c>, which indicates an unregistered projection type.
    ///         When <see cref="NotFound" /> is returned, the subscription is still valid—the entity
    ///         simply has no events yet. The client should continue listening for SignalR updates.
    ///     </para>
    /// </remarks>
    public static ProjectionFetchResult NotFound { get; } = new()
    {
        IsNotFound = true,
    };

    /// <summary>
    ///     Gets the projection data object, or <c>null</c> if <see cref="IsNotFound" /> is <c>true</c>.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the projection was not found (no events exist yet).
    /// </summary>
    /// <value>
    ///     <c>true</c> if the server returned 404 (no events for this entity);
    ///     otherwise, <c>false</c>.
    /// </value>
    public bool IsNotFound { get; init; }

    /// <summary>
    ///     Gets the version/etag of the projection for optimistic concurrency.
    /// </summary>
    /// <value>
    ///     The projection version, or <c>0</c> if <see cref="IsNotFound" /> is <c>true</c>.
    /// </value>
    public long Version { get; init; }

    /// <summary>
    ///     Creates a typed fetch result with projection data.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="data">The projection data.</param>
    /// <param name="version">The projection version.</param>
    /// <returns>A new fetch result containing the projection data.</returns>
    public static ProjectionFetchResult Create<T>(
        T data,
        long version
    )
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(data);
        return new()
        {
            Data = data,
            Version = version,
        };
    }

    /// <summary>
    ///     Creates a fetch result from an object and version.
    /// </summary>
    /// <param name="data">The projection data.</param>
    /// <param name="version">The projection version.</param>
    /// <returns>A new fetch result containing the projection data.</returns>
    public static ProjectionFetchResult Create(
        object data,
        long version
    )
    {
        ArgumentNullException.ThrowIfNull(data);
        return new()
        {
            Data = data,
            Version = version,
        };
    }
}
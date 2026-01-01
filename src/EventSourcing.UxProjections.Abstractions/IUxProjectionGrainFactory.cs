using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Abstractions;

/// <summary>
///     Factory for resolving UX projection grains.
/// </summary>
/// <remarks>
///     <para>
///         The factory abstracts grain resolution and key construction, allowing consumers
///         to request projection grains using domain types rather than string keys.
///     </para>
/// </remarks>
public interface IUxProjectionGrainFactory
{
    /// <summary>
    ///     Gets a UX projection cursor grain for the specified key.
    /// </summary>
    /// <param name="key">The UX projection key.</param>
    /// <returns>A grain reference for the UX projection cursor.</returns>
    IUxProjectionCursorGrain GetUxProjectionCursorGrain(
        UxProjectionKey key
    );

    /// <summary>
    ///     Gets a UX projection cursor grain for the specified projection type and grain with a brook name attribute.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <typeparam name="TGrain">
    ///     The grain type decorated with
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </typeparam>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <returns>A grain reference for the UX projection cursor.</returns>
    IUxProjectionCursorGrain GetUxProjectionCursorGrainForGrain<TProjection, TGrain>(
        string entityId
    )
        where TGrain : class;

    /// <summary>
    ///     Gets a UX projection grain for the specified key.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <param name="key">The UX projection key.</param>
    /// <returns>A grain reference for the UX projection.</returns>
    IUxProjectionGrain<TProjection> GetUxProjectionGrain<TProjection>(
        UxProjectionKey key
    );

    /// <summary>
    ///     Gets a UX projection grain for the specified projection type and grain with a brook name attribute.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <typeparam name="TGrain">
    ///     The grain type decorated with
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </typeparam>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <returns>A grain reference for the UX projection.</returns>
    IUxProjectionGrain<TProjection> GetUxProjectionGrainForGrain<TProjection, TGrain>(
        string entityId
    )
        where TGrain : class;

    /// <summary>
    ///     Gets a versioned UX projection cache grain for the specified key.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <param name="key">The versioned UX projection key.</param>
    /// <returns>A grain reference for the versioned UX projection cache.</returns>
    IUxProjectionVersionedCacheGrain<TProjection> GetUxProjectionVersionedCacheGrain<TProjection>(
        UxProjectionVersionedKey key
    );

    /// <summary>
    ///     Gets a versioned UX projection cache grain for the specified projection type, grain, and version.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <typeparam name="TGrain">
    ///     The grain type decorated with
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </typeparam>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <param name="version">The specific version to retrieve.</param>
    /// <returns>A grain reference for the versioned UX projection cache.</returns>
    IUxProjectionVersionedCacheGrain<TProjection> GetUxProjectionVersionedCacheGrainForGrain<TProjection, TGrain>(
        string entityId,
        BrookPosition version
    )
        where TGrain : class;
}
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
    /// <param name="key">The UX projection cursor key.</param>
    /// <returns>A grain reference for the UX projection cursor.</returns>
    IUxProjectionCursorGrain GetUxProjectionCursorGrain(
        UxProjectionCursorKey key
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
    ///     Gets a UX projection grain for the specified entity ID.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>A grain reference for the UX projection.</returns>
    /// <remarks>
    ///     The grain is keyed by just the entity ID. The brook name is obtained from
    ///     the <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
    ///     on the concrete grain class, and the projection type is known from <typeparamref name="TProjection" />.
    /// </remarks>
    IUxProjectionGrain<TProjection> GetUxProjectionGrain<TProjection>(
        string entityId
    );

    /// <summary>
    ///     Gets a versioned UX projection cache grain for the specified key.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <param name="key">The versioned UX projection cache key.</param>
    /// <returns>A grain reference for the versioned UX projection cache.</returns>
    IUxProjectionVersionedCacheGrain<TProjection> GetUxProjectionVersionedCacheGrain<TProjection>(
        UxProjectionVersionedCacheKey key
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
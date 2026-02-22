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
///     <para>
///         Projection types must be decorated with
///         <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
///         to identify which brook they read from.
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
    ///     Gets a UX projection cursor grain for the specified projection type.
    /// </summary>
    /// <typeparam name="TProjection">
    ///     The projection state type, decorated with
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </typeparam>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <returns>A grain reference for the UX projection cursor.</returns>
    IUxProjectionCursorGrain GetUxProjectionCursorGrain<TProjection>(
        string entityId
    )
        where TProjection : class;

    /// <summary>
    ///     Gets a UX projection grain for the specified entity ID.
    /// </summary>
    /// <typeparam name="TProjection">
    ///     The projection state type, decorated with
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>A grain reference for the UX projection.</returns>
    /// <remarks>
    ///     The grain is keyed by just the entity ID. The brook name is obtained from
    ///     the <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
    ///     on the <typeparamref name="TProjection" /> type itself.
    /// </remarks>
    IUxProjectionGrain<TProjection> GetUxProjectionGrain<TProjection>(
        string entityId
    )
        where TProjection : class;

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
    ///     Gets a versioned UX projection cache grain for the specified projection type and version.
    /// </summary>
    /// <typeparam name="TProjection">
    ///     The projection state type, decorated with
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </typeparam>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <param name="version">The specific version to retrieve.</param>
    /// <returns>A grain reference for the versioned UX projection cache.</returns>
    IUxProjectionVersionedCacheGrain<TProjection> GetUxProjectionVersionedCacheGrain<TProjection>(
        string entityId,
        BrookPosition version
    )
        where TProjection : class;
}
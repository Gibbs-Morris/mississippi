using Mississippi.EventSourcing.Abstractions;


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
    ///     Gets a UX projection grain for the specified projection type and brook.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <typeparam name="TBrook">The brook definition type.</typeparam>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <returns>A grain reference for the UX projection.</returns>
    IUxProjectionGrain<TProjection> GetUxProjectionGrain<TProjection, TBrook>(
        string entityId
    )
        where TBrook : IBrookDefinition;

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
    ///     Gets a UX projection cursor grain for the specified projection type and brook.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <typeparam name="TBrook">The brook definition type.</typeparam>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <returns>A grain reference for the UX projection cursor.</returns>
    IUxProjectionCursorGrain GetUxProjectionCursorGrain<TProjection, TBrook>(
        string entityId
    )
        where TBrook : IBrookDefinition;

    /// <summary>
    ///     Gets a UX projection cursor grain for the specified key.
    /// </summary>
    /// <param name="key">The UX projection key.</param>
    /// <returns>A grain reference for the UX projection cursor.</returns>
    IUxProjectionCursorGrain GetUxProjectionCursorGrain(
        UxProjectionKey key
    );
}

using Mississippi.Core.Abstractions.Cqrs.Query;


namespace Mississippi.Client;

/// <summary>
///     Represents a client-side cache of projections that are mirrored from the Orleans cluster.
///     Consumers can subscribe to a particular <see cref="QueryReference" /> to receive real-time updates
///     and retrieve the latest local copy of the projection state.
/// </summary>
public interface IMirroredProjectionState
{
    /// <summary>
    ///     Retrieves the most recent local state associated with the specified <paramref name="queryReference" />.
    /// </summary>
    /// <typeparam name="T">CLR type of the projection.</typeparam>
    /// <param name="queryReference">The query reference that uniquely identifies the projection.</param>
    /// <returns>The projection instance if it exists in the cache; otherwise, <c>null</c>.</returns>
    T? GetState<T>(
        QueryReference queryReference
    )
        where T : class;

    /// <summary>
    ///     Begins streaming updates for the projection identified by <paramref name="queryReference" />.
    /// </summary>
    /// <param name="queryReference">The query reference to subscribe to.</param>
    Task SubscribeAsync(
        QueryReference queryReference
    );

    /// <summary>
    ///     Stops streaming updates for the projection identified by <paramref name="queryReference" /> and removes any cached
    ///     state.
    /// </summary>
    /// <param name="queryReference">The query reference to unsubscribe from.</param>
    Task UnsubscribeAsync(
        QueryReference queryReference
    );
}
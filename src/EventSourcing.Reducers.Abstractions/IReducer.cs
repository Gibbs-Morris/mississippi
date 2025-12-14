namespace Mississippi.EventSourcing.Reducers.Abstractions;

/// <summary>
///     Defines a reducer that attempts to transform an incoming event into a projection.
/// </summary>
/// <typeparam name="TProjection">The projection type produced by the reducer.</typeparam>
public interface IReducer<TProjection>
{
    /// <summary>
    ///     Attempts to apply an event to the current projection to produce the next projection.
    /// </summary>
    /// <param name="state">The current projection state.</param>
    /// <param name="eventData">The event to reduce.</param>
    /// <param name="projection">The projection produced from the event when reduction succeeds.</param>
    /// <returns>
    ///     A value indicating whether the reducer can handle the supplied event and produced a projection.
    /// </returns>
    bool TryReduce(
        TProjection state,
        object eventData,
        out TProjection projection
    );
}

/// <summary>
///     Defines a reducer that transforms an incoming event into a projection.
/// </summary>
/// <typeparam name="TEvent">The event type consumed by the reducer.</typeparam>
/// <typeparam name="TProjection">The projection type produced by the reducer.</typeparam>
public interface IReducer<in TEvent, TProjection> : IReducer<TProjection>
{
    /// <summary>
    ///     Applies an event to the current projection to produce the next projection.
    /// </summary>
    /// <param name="state">The current projection state.</param>
    /// <param name="eventData">The event to reduce.</param>
    /// <returns>The projection produced from applying the event.</returns>
    TProjection Reduce(
        TProjection state,
        TEvent eventData
    );
}
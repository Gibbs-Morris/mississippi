namespace Mississippi.EventSourcing.Reducer;

/// <summary>
///     Optional internal interface exposing the concrete event type a reducer handles.
///     The root reducer exploits this to achieve <c>O(1)</c> dispatch.
/// </summary>
internal interface IEventAwareReducer
{
    /// <summary>
    ///     Gets the event type the reducer can handle.
    /// </summary>
    Type SupportedEventType { get; }

    /// <summary>
    ///     Determines whether the reducer can handle the supplied <paramref name="eventType" />.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns><see langword="true" /> if it can handle; otherwise <see langword="false" />.</returns>
    bool CanHandle(
        Type eventType
    );
}
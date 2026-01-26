namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Marker interface for saga definitions.
///     Saga classes should implement this to be discoverable by the framework.
/// </summary>
public interface ISagaDefinition
{
    /// <summary>
    ///     Gets the saga name for identification and event storage.
    /// </summary>
    static abstract string SagaName { get; }
}

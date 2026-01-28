using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Context provided to saga steps during execution.
/// </summary>
public interface ISagaContext
{
    /// <summary>
    ///     Gets the current attempt number for the executing step (1-based).
    /// </summary>
    int Attempt { get; }

    /// <summary>
    ///     Gets the correlation identifier linking related operations.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    ///     Gets the unique identifier for this saga instance.
    /// </summary>
    Guid SagaId { get; }

    /// <summary>
    ///     Gets the name of the saga type being executed.
    /// </summary>
    string SagaName { get; }

    /// <summary>
    ///     Gets the timestamp when the saga was started.
    /// </summary>
    DateTimeOffset StartedAt { get; }
}
using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.Abstractions.Actions;

/// <summary>
///     Represents an action dispatched when a saga start request completes successfully.
/// </summary>
/// <remarks>
///     <para>
///         Each saga type should have its own implementing action (e.g., <c>StartTransferFundsSagaSucceededAction</c>)
///         to enable per-saga tracking, history, and correlation.
///     </para>
///     <para>
///         Implementations must provide a static factory method via the
///         <see cref="ISagaSucceededAction{TSelf}" /> interface for factory creation.
///     </para>
///     <para>
///         Note: This indicates the saga was successfully <em>started</em>, not that it has completed.
///         Saga completion is tracked via the saga status projection.
///     </para>
/// </remarks>
public interface ISagaSucceededAction : IAction
{
    /// <summary>
    ///     Gets the unique identifier for this saga instance.
    /// </summary>
    /// <remarks>
    ///     Correlates with the <see cref="ISagaExecutingAction.SagaId" /> from the initiating action.
    /// </remarks>
    Guid SagaId { get; }

    /// <summary>
    ///     Gets the timestamp when the saga start request completed successfully.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}

/// <summary>
///     Extends <see cref="ISagaSucceededAction" /> with a static factory method for creating instances.
/// </summary>
/// <typeparam name="TSelf">The implementing type (CRTP pattern).</typeparam>
public interface ISagaSucceededAction<TSelf> : ISagaSucceededAction
    where TSelf : ISagaSucceededAction<TSelf>
{
    /// <summary>
    ///     Creates a new instance of the succeeded action.
    /// </summary>
    /// <param name="sagaId">The unique saga instance identifier.</param>
    /// <param name="timestamp">The timestamp when the saga start request completed.</param>
    /// <returns>A new instance of the succeeded action.</returns>
    static abstract TSelf Create(
        Guid sagaId,
        DateTimeOffset timestamp
    );
}
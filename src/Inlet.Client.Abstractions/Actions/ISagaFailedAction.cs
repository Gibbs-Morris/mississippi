using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.Abstractions.Actions;

/// <summary>
///     Represents an action dispatched when a saga start request fails.
/// </summary>
/// <remarks>
///     <para>
///         Each saga type should have its own implementing action (e.g., <c>StartTransferFundsSagaFailedAction</c>)
///         to enable per-saga tracking, history, and correlation.
///     </para>
///     <para>
///         Implementations must provide a static factory method via the
///         <see cref="ISagaFailedAction{TSelf}" /> interface for factory creation.
///     </para>
/// </remarks>
public interface ISagaFailedAction : IAction
{
    /// <summary>
    ///     Gets the error code describing the failure.
    /// </summary>
    string? ErrorCode { get; }

    /// <summary>
    ///     Gets a human-readable description of the error.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    ///     Gets the unique identifier for this saga instance.
    /// </summary>
    /// <remarks>
    ///     Correlates with the <see cref="ISagaExecutingAction.SagaId" /> from the initiating action.
    /// </remarks>
    Guid SagaId { get; }

    /// <summary>
    ///     Gets the timestamp when the saga start request failed.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}

/// <summary>
///     Extends <see cref="ISagaFailedAction" /> with a static factory method for creating instances.
/// </summary>
/// <typeparam name="TSelf">The implementing type (CRTP pattern).</typeparam>
public interface ISagaFailedAction<TSelf> : ISagaFailedAction
    where TSelf : ISagaFailedAction<TSelf>
{
    /// <summary>
    ///     Creates a new instance of the failed action.
    /// </summary>
    /// <param name="sagaId">The unique saga instance identifier.</param>
    /// <param name="errorCode">The error code describing the failure.</param>
    /// <param name="errorMessage">A human-readable description of the error.</param>
    /// <param name="timestamp">The timestamp when the saga start request failed.</param>
    /// <returns>A new instance of the failed action.</returns>
    static abstract TSelf Create(
        Guid sagaId,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset timestamp
    );
}
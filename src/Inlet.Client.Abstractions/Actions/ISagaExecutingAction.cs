using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.Abstractions.Actions;

/// <summary>
///     Represents an action dispatched when a saga starts executing.
/// </summary>
/// <remarks>
///     <para>
///         Each saga type should have its own implementing action (e.g., <c>StartTransferFundsSagaExecutingAction</c>)
///         to enable per-saga tracking, history, and correlation.
///     </para>
///     <para>
///         Implementations must provide a static factory method via the
///         <see cref="ISagaExecutingAction{TSelf}" /> interface for factory creation.
///     </para>
/// </remarks>
public interface ISagaExecutingAction : IAction
{
    /// <summary>
    ///     Gets the unique identifier for this saga instance.
    /// </summary>
    /// <remarks>
    ///     Used to correlate executing, succeeded, and failed actions for the same saga instance.
    /// </remarks>
    Guid SagaId { get; }

    /// <summary>
    ///     Gets the name of the saga type being executed.
    /// </summary>
    string SagaType { get; }

    /// <summary>
    ///     Gets the timestamp when the saga started executing.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}

/// <summary>
///     Extends <see cref="ISagaExecutingAction" /> with a static factory method for creating instances.
/// </summary>
/// <typeparam name="TSelf">The implementing type (CRTP pattern).</typeparam>
public interface ISagaExecutingAction<TSelf> : ISagaExecutingAction
    where TSelf : ISagaExecutingAction<TSelf>
{
    /// <summary>
    ///     Creates a new instance of the executing action.
    /// </summary>
    /// <param name="sagaId">The unique saga instance identifier.</param>
    /// <param name="sagaType">The name of the saga type.</param>
    /// <param name="timestamp">The timestamp when the saga started.</param>
    /// <returns>A new instance of the executing action.</returns>
    static abstract TSelf Create(
        Guid sagaId,
        string sagaType,
        DateTimeOffset timestamp
    );
}
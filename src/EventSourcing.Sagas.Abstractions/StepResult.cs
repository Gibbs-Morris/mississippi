using System;
using System.Collections.Generic;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Represents the result of a saga step execution.
/// </summary>
/// <remarks>
///     <para>
///         Steps return this result to indicate success or failure and optionally emit
///         business events that update the saga state. Lifecycle events (started/completed/failed)
///         are emitted automatically by the saga infrastructure.
///     </para>
/// </remarks>
public sealed record StepResult
{
    private static readonly StepResult SucceededInstance = new() { Success = true };

    /// <summary>
    ///     Gets a value indicating whether the step executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Gets the error code when the step failed.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    ///     Gets the error message when the step failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the business events produced by the step.
    ///     These events are persisted to update the saga state.
    /// </summary>
    public IReadOnlyList<object> Events { get; init; } = [];

    /// <summary>
    ///     Creates a successful step result with no events.
    /// </summary>
    /// <returns>A successful <see cref="StepResult" />.</returns>
    public static StepResult Succeeded() => SucceededInstance;

    /// <summary>
    ///     Creates a successful step result with business events.
    /// </summary>
    /// <param name="events">The business events to persist.</param>
    /// <returns>A successful <see cref="StepResult" /> with the specified events.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="events" /> is null.</exception>
    public static StepResult Succeeded(
        params object[] events
    )
    {
        ArgumentNullException.ThrowIfNull(events);
        return new StepResult { Success = true, Events = events };
    }

    /// <summary>
    ///     Creates a successful step result with business events.
    /// </summary>
    /// <param name="events">The business events to persist.</param>
    /// <returns>A successful <see cref="StepResult" /> with the specified events.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="events" /> is null.</exception>
    public static StepResult Succeeded(
        IReadOnlyList<object> events
    )
    {
        ArgumentNullException.ThrowIfNull(events);
        return new StepResult { Success = true, Events = events };
    }

    /// <summary>
    ///     Creates a failed step result.
    /// </summary>
    /// <param name="errorCode">The error code identifying the failure.</param>
    /// <param name="errorMessage">An optional message describing the failure.</param>
    /// <returns>A failed <see cref="StepResult" />.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errorCode" /> is null or whitespace.</exception>
    public static StepResult Failed(
        string errorCode,
        string? errorMessage = null
    )
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException("Error code cannot be null or whitespace.", nameof(errorCode));
        }

        return new StepResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
        };
    }
}

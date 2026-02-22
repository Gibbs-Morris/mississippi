using System.Collections.Generic;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Represents the outcome of a saga step execution.
/// </summary>
public sealed record StepResult
{
    /// <summary>
    ///     Gets the error code when the step fails.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    ///     Gets the error message when the step fails.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the events emitted by the step.
    /// </summary>
    public IReadOnlyList<object> Events { get; init; } = [];

    /// <summary>
    ///     Gets a value indicating whether the step succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Creates a failed step result.
    /// </summary>
    /// <param name="errorCode">The error code describing the failure.</param>
    /// <param name="message">The optional error message.</param>
    /// <returns>The failed result.</returns>
    public static StepResult Failed(
        string errorCode,
        string? message = null
    ) =>
        new()
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = message,
        };

    /// <summary>
    ///     Creates a successful step result with optional events.
    /// </summary>
    /// <param name="events">The events emitted by the step.</param>
    /// <returns>The successful result.</returns>
    public static StepResult Succeeded(
        params object[] events
    ) =>
        new()
        {
            Success = true,
            Events = events,
        };
}
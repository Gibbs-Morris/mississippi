using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Represents the result of a saga compensation execution.
/// </summary>
/// <remarks>
///     <para>
///         Compensations return this result to indicate whether the undo operation
///         succeeded, was skipped (nothing to undo), or failed.
///     </para>
/// </remarks>
public sealed record CompensationResult
{
    private static readonly CompensationResult SucceededInstance = new() { Success = true };
    private static readonly CompensationResult SkippedInstance = new() { Success = true, WasSkipped = true };

    /// <summary>
    ///     Gets a value indicating whether the compensation executed or skipped successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the compensation was skipped because there was nothing to undo.
    /// </summary>
    public bool WasSkipped { get; init; }

    /// <summary>
    ///     Gets the error code when the compensation failed.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    ///     Gets the error message when the compensation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Creates a successful compensation result.
    /// </summary>
    /// <returns>A successful <see cref="CompensationResult" />.</returns>
    public static CompensationResult Succeeded() => SucceededInstance;

    /// <summary>
    ///     Creates a skipped compensation result indicating there was nothing to undo.
    /// </summary>
    /// <returns>A skipped <see cref="CompensationResult" />.</returns>
    public static CompensationResult Skipped() => SkippedInstance;

    /// <summary>
    ///     Creates a failed compensation result.
    /// </summary>
    /// <param name="errorCode">The error code identifying the failure.</param>
    /// <param name="errorMessage">An optional message describing the failure.</param>
    /// <returns>A failed <see cref="CompensationResult" />.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errorCode" /> is null or whitespace.</exception>
    public static CompensationResult Failed(
        string errorCode,
        string? errorMessage = null
    )
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException("Error code cannot be null or whitespace.", nameof(errorCode));
        }

        return new CompensationResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
        };
    }
}

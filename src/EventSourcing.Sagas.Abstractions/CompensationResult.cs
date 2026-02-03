namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Represents the outcome of a compensation step.
/// </summary>
public sealed record CompensationResult
{
    /// <summary>
    ///     Gets a value indicating whether the compensation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the compensation was skipped.
    /// </summary>
    public bool Skipped { get; init; }

    /// <summary>
    ///     Gets the error code when the compensation fails.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    ///     Gets the error message when the compensation fails or is skipped.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Creates a successful compensation result.
    /// </summary>
    /// <returns>The successful result.</returns>
    public static CompensationResult Succeeded() => new() { Success = true };

    /// <summary>
    ///     Creates a skipped compensation result.
    /// </summary>
    /// <param name="reason">The optional reason for skipping.</param>
    /// <returns>The skipped result.</returns>
    public static CompensationResult SkippedResult(string? reason = null) =>
        new() { Skipped = true, ErrorMessage = reason };

    /// <summary>
    ///     Creates a failed compensation result.
    /// </summary>
    /// <param name="errorCode">The error code describing the failure.</param>
    /// <param name="message">The optional error message.</param>
    /// <returns>The failed result.</returns>
    public static CompensationResult Failed(string errorCode, string? message = null) =>
        new() { Success = false, ErrorCode = errorCode, ErrorMessage = message };
}

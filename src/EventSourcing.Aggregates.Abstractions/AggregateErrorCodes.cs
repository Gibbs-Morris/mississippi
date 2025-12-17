namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Provides well-known error codes for common aggregate operation failures.
/// </summary>
/// <remarks>
///     Use these constants when returning <see cref="OperationResult.Fail" /> from command handlers
///     to ensure consistent error handling across the application.
/// </remarks>
public static class AggregateErrorCodes
{
    /// <summary>
    ///     The aggregate already exists when attempting to create it.
    /// </summary>
    public const string AlreadyExists = "AGGREGATE_ALREADY_EXISTS";

    /// <summary>
    ///     The command handler for the specified command type was not found.
    /// </summary>
    public const string CommandHandlerNotFound = "COMMAND_HANDLER_NOT_FOUND";

    /// <summary>
    ///     The optimistic concurrency check failed due to a version mismatch.
    /// </summary>
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";

    /// <summary>
    ///     The command is invalid or contains invalid data.
    /// </summary>
    public const string InvalidCommand = "INVALID_COMMAND";

    /// <summary>
    ///     The aggregate is in an invalid state for the requested operation.
    /// </summary>
    public const string InvalidState = "INVALID_STATE";

    /// <summary>
    ///     The aggregate was not found when it was expected to exist.
    /// </summary>
    public const string NotFound = "AGGREGATE_NOT_FOUND";
}
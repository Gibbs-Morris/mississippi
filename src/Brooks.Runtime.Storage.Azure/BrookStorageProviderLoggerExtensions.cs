using Microsoft.Extensions.Logging;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     High-performance logging extensions for the Brooks Azure provider shell.
/// </summary>
internal static partial class BrookStorageProviderLoggerExtensions
{
    /// <summary>
    ///     Logs when an Increment 2 operation is invoked before the event implementation is available.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The requested operation name.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Brooks Azure storage operation '{OperationName}' was invoked before the Increment 2 event implementation was added")]
    public static partial void OperationNotYetAvailable(
        this ILogger logger,
        string operationName
    );
}
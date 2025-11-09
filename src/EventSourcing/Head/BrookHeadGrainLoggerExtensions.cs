using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Head;

/// <summary>
///     High-performance logging extensions for <see cref="BrookHeadGrain" />.
/// </summary>
internal static class BrookHeadGrainLoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> InvalidPrimaryKeyMessage = LoggerMessage.Define<string>(
        LogLevel.Error,
        new(1, nameof(InvalidPrimaryKey)),
        "Failed to parse brook head grain primary key '{PrimaryKey}'.");

    /// <summary>
    ///     Logs that the grain received an invalid primary key value.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="primaryKey">The invalid primary key.</param>
    /// <param name="exception">The exception thrown during parsing.</param>
    public static void InvalidPrimaryKey(
        this ILogger<BrookHeadGrain> logger,
        string primaryKey,
        Exception? exception
    ) =>
        InvalidPrimaryKeyMessage(logger, primaryKey, exception);
}
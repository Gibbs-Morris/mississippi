using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Brooks.Cursor;

/// <summary>
///     High-performance logging extensions for <see cref="BrookCursorGrain" />.
/// </summary>
internal static class BrookCursorGrainLoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> InvalidPrimaryKeyMessage = LoggerMessage.Define<string>(
        LogLevel.Error,
        new(1, nameof(InvalidPrimaryKey)),
        "Failed to parse brook cursor grain primary key '{PrimaryKey}'.");

    /// <summary>
    ///     Logs that the grain received an invalid primary key value.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="primaryKey">The invalid primary key.</param>
    /// <param name="exception">The exception thrown during parsing.</param>
    public static void InvalidPrimaryKey(
        this ILogger<BrookCursorGrain> logger,
        string primaryKey,
        Exception? exception
    ) =>
        InvalidPrimaryKeyMessage(logger, primaryKey, exception);
}
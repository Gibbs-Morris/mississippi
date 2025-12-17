using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Brooks.Cursor;

/// <summary>
///     High-performance logging extensions for <see cref="BrookCursorGrain" />.
/// </summary>
internal static partial class BrookCursorGrainLoggerExtensions
{
    /// <summary>
    ///     Logs that the grain received an invalid primary key value.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="primaryKey">The invalid primary key.</param>
    /// <param name="exception">The exception thrown during parsing.</param>
    [LoggerMessage(1, LogLevel.Error, "Failed to parse brook cursor grain primary key '{PrimaryKey}'.")]
    public static partial void InvalidPrimaryKey(
        this ILogger logger,
        string primaryKey,
        Exception? exception
    );
}
using System;

using Microsoft.Extensions.Logging;


namespace Crescent.ConsoleApp.Infrastructure;

/// <summary>
///     Strongly-typed logging extension methods for the console app, implemented via LoggerMessage.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load run state from {Path}")]
    public static partial void FailedToLoadRunState(
        this ILogger logger,
        Exception exception,
        string path
    );

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse run state JSON from {Path}")]
    public static partial void FailedToParseRunStateJson(
        this ILogger logger,
        Exception exception,
        string path
    );

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to save run state to {Path}")]
    public static partial void FailedToSaveRunState(
        this ILogger logger,
        Exception exception,
        string path
    );

    [LoggerMessage(Level = LogLevel.Warning, Message = "No access to save run state to {Path}")]
    public static partial void NoAccessToSaveRunState(
        this ILogger logger,
        Exception exception,
        string path
    );
}
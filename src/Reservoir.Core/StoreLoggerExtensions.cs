using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Source-generated logging for isolated store listener failures.
/// </summary>
internal static partial class StoreLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        EventName = "ListenerFailed",
        Level = LogLevel.Error,
        Message = "Store listener failed; continuing notification.")]
    public static partial void ListenerFailed(
        this ILogger logger,
        Exception exception
    );
}

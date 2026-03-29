using Microsoft.Extensions.Logging;


namespace Mississippi.Refraction.Client.Infrastructure;

/// <summary>
///     Defines RefractionRoot logging messages.
/// </summary>
internal static partial class RefractionRootLoggerExtensions
{
    /// <summary>
    ///     Logs fallback from an unknown theme selection to the default theme.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestedTheme">The requested theme identifier.</param>
    /// <param name="defaultTheme">The default theme identifier.</param>
    [LoggerMessage(
        EventId = 1200,
        Level = LogLevel.Warning,
        Message =
            "Refraction theme '{RequestedTheme}' is not registered. Falling back to default theme '{DefaultTheme}'.")]
    internal static partial void UnknownThemeFallback(
        this ILogger logger,
        string requestedTheme,
        string defaultTheme
    );
}
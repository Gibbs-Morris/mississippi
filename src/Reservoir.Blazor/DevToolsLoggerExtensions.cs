using Microsoft.Extensions.Logging;


namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     Logger extensions for Reservoir DevTools.
/// </summary>
internal static partial class DevToolsLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "DevTools is registered and enabled but Initialize() was not called. " +
                  "Add <ReservoirDevToolsInitializerComponent/> to your App.razor or root layout. " +
                  "Without this component, Redux DevTools will not connect to your application.")]
    public static partial void DevToolsNotInitialized(
        this ILogger logger
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "DevTools initialization check: WasInitialized={WasInitialized}")]
    public static partial void DevToolsInitializationCheckResult(
        this ILogger logger,
        bool wasInitialized
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "DevTools is registered and enabled but Initialize() was not called. " +
                  "Add <ReservoirDevToolsInitializerComponent/> to your App.razor or root layout. " +
                  "Set ThrowOnMissingInitializer to false in ReservoirDevToolsOptions to log a warning instead.")]
    public static partial void DevToolsNotInitializedError(
        this ILogger logger
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Redux DevTools connected successfully.")]
    public static partial void DevToolsConnected(
        this ILogger logger
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Failed to connect to Redux DevTools. " +
                  "Ensure the Redux DevTools browser extension is installed and the page is open in a supported browser.")]
    public static partial void DevToolsConnectionFailed(
        this ILogger logger
    );
}

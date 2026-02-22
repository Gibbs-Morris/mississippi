namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     Tracks whether DevTools has been initialized by the component.
/// </summary>
/// <remarks>
///     This singleton tracker communicates between the scoped <see cref="ReduxDevToolsService" />
///     and the singleton <see cref="DevToolsInitializationCheckerService" />.
/// </remarks>
internal sealed class DevToolsInitializationTracker
{
    private volatile bool wasInitialized;

    /// <summary>
    ///     Gets or sets a value indicating whether <see cref="ReduxDevToolsService.Initialize" /> was called.
    /// </summary>
    public bool WasInitialized
    {
        get => wasInitialized;
        set => wasInitialized = value;
    }
}
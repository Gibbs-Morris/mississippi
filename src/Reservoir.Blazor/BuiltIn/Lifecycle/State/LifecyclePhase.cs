namespace Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.State;

/// <summary>
///     Represents the current phase of the application lifecycle.
/// </summary>
public enum LifecyclePhase
{
    /// <summary>
    ///     The application has not yet started initialization.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    ///     The application is currently initializing.
    /// </summary>
    /// <remarks>
    ///     This phase begins when <see cref="Actions.AppInitAction" /> is dispatched.
    /// </remarks>
    Initializing = 1,

    /// <summary>
    ///     The application has completed initialization and is ready for use.
    /// </summary>
    /// <remarks>
    ///     This phase begins when <see cref="Actions.AppReadyAction" /> is dispatched.
    /// </remarks>
    Ready = 2,
}
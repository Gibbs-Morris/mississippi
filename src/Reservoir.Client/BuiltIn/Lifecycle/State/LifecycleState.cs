using System;

using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.State;

/// <summary>
///     Feature state for tracking application lifecycle in a Reservoir store.
/// </summary>
/// <remarks>
///     <para>
///         This state tracks the current lifecycle phase of the application.
///         It is updated by reducers when lifecycle actions are dispatched.
///     </para>
///     <para>
///         Components can use this state to:
///     </para>
///     <list type="bullet">
///         <item>Show loading indicators during initialization</item>
///         <item>Prevent user interaction until the app is ready</item>
///         <item>Track when initialization started for performance metrics</item>
///     </list>
/// </remarks>
public sealed record LifecycleState : IFeatureState
{
    /// <summary>
    ///     The feature key used to identify this state in the store.
    /// </summary>
    public const string Key = "reservoir:lifecycle";

    /// <inheritdoc />
    public static string FeatureKey => Key;

    /// <summary>
    ///     Gets the timestamp when initialization started.
    /// </summary>
    /// <remarks>
    ///     Set when <see cref="Actions.AppInitAction" /> is dispatched.
    ///     Null if initialization has not started.
    /// </remarks>
    public DateTimeOffset? InitializedAt { get; init; }

    /// <summary>
    ///     Gets the current lifecycle phase of the application.
    /// </summary>
    public LifecyclePhase Phase { get; init; } = LifecyclePhase.NotStarted;

    /// <summary>
    ///     Gets the timestamp when the application became ready.
    /// </summary>
    /// <remarks>
    ///     Set when <see cref="Actions.AppReadyAction" /> is dispatched.
    ///     Null if the application is not yet ready.
    /// </remarks>
    public DateTimeOffset? ReadyAt { get; init; }
}
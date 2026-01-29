using System;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Reducers;

/// <summary>
///     Reducer for <see cref="AppInitAction" /> that captures timestamps using a configured <see cref="TimeProvider" />.
/// </summary>
public sealed class AppInitReducer : ActionReducerBase<AppInitAction, LifecycleState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AppInitReducer" /> class.
    /// </summary>
    /// <param name="timeProvider">The time provider used for timestamps.</param>
    public AppInitReducer(
        TimeProvider timeProvider
    )
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        TimeProvider = timeProvider;
    }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public override LifecycleState Reduce(
        LifecycleState state,
        AppInitAction action
    ) =>
        LifecycleReducers.OnAppInit(state, action, TimeProvider);
}
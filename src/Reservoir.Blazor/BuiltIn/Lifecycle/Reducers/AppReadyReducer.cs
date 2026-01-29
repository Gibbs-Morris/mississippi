using System;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Reducers;

/// <summary>
///     Reducer for <see cref="AppReadyAction" /> that captures timestamps using a configured <see cref="TimeProvider" />.
/// </summary>
public sealed class AppReadyReducer : ActionReducerBase<AppReadyAction, LifecycleState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AppReadyReducer" /> class.
    /// </summary>
    /// <param name="timeProvider">The time provider used for timestamps.</param>
    public AppReadyReducer(
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
        AppReadyAction action
    ) =>
        LifecycleReducers.OnAppReady(state, action, TimeProvider);
}
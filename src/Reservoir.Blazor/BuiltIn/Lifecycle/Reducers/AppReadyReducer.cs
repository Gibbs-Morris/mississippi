using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Reducers;

/// <summary>
///     Reducer for <see cref="AppReadyAction" />.
/// </summary>
public sealed class AppReadyReducer : ActionReducerBase<AppReadyAction, LifecycleState>
{
    /// <inheritdoc />
    public override LifecycleState Reduce(
        LifecycleState state,
        AppReadyAction action
    ) =>
        LifecycleReducers.OnAppReady(state, action);
}
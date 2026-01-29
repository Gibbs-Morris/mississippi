using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Reducers;

/// <summary>
///     Reducer for <see cref="AppInitAction" />.
/// </summary>
public sealed class AppInitReducer : ActionReducerBase<AppInitAction, LifecycleState>
{
    /// <inheritdoc />
    public override LifecycleState Reduce(
        LifecycleState state,
        AppInitAction action
    ) =>
        LifecycleReducers.OnAppInit(state, action);
}
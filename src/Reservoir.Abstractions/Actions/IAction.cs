namespace Mississippi.Reservoir.Abstractions.Actions;

/// <summary>
///     Marker interface for all actions in the Reservoir state management system.
///     Actions represent events that trigger state changes or effects.
/// </summary>
/// <remarks>
///     <para>
///         Actions are dispatched to the <see cref="IStore" /> and processed by reducers
///         and effects. Reducers handle synchronous state transitions; effects handle async operations.
///     </para>
///     <para>
///         Actions should be immutable records that describe what happened or what the user intends.
///         They carry the minimal data needed for reducers and effects to do their work.
///     </para>
/// </remarks>
public interface IAction
{
}
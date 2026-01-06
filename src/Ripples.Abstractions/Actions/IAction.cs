namespace Mississippi.Ripples.Abstractions.Actions;

/// <summary>
///     Marker interface for all actions in the Ripples state management system.
///     Actions represent events that can change state (local) or trigger effects (server-synced).
/// </summary>
/// <remarks>
///     <para>
///         Unlike <see cref="IRippleAction" /> which requires an EntityId for server-synced projections,
///         <see cref="IAction" /> supports both local-only state changes (UI state, forms) and
///         server-synced operations (projections, commands).
///     </para>
///     <para>
///         Actions are dispatched to the <see cref="IRippleStore" /> and processed by reducers
///         and effects. Reducers handle synchronous state transitions; effects handle async operations.
///     </para>
/// </remarks>
public interface IAction
{
}
namespace Mississippi.Reservoir.Abstractions.Actions;

/// <summary>
///     Marker interface for system actions that control store behavior.
/// </summary>
/// <remarks>
///     <para>
///         System actions are handled directly by the store rather than by user-defined reducers.
///         They enable external components (like DevTools) to command the store through the
///         standard dispatch mechanism, maintaining unidirectional data flow.
///     </para>
///     <para>
///         System actions do not trigger user reducers or effects. They are processed
///         internally by the store and emit appropriate <see cref="Events.StoreEventBase" />s
///         to notify observers of what happened.
///     </para>
/// </remarks>
public interface ISystemAction : IAction
{
}
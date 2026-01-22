using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.EntitySelection;

/// <summary>
///     Action dispatched to set the currently selected entity ID.
/// </summary>
/// <remarks>
///     <para>
///         This action is dispatched when the user navigates to or selects a specific entity.
///         It updates <see cref="EntitySelectionState.EntityId" /> which is then used when
///         dispatching commands to target the correct aggregate.
///     </para>
///     <para>
///         Pass an empty string to clear the selection.
///     </para>
/// </remarks>
/// <param name="EntityId">The entity ID to select, or empty string to clear selection.</param>
internal sealed record SetEntityIdAction(string EntityId) : IAction;
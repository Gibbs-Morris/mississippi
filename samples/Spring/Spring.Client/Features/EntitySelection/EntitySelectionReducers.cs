namespace Spring.Client.Features.EntitySelection;

/// <summary>
///     Pure reducer functions for the EntitySelection feature state.
/// </summary>
internal static class EntitySelectionReducers
{
    /// <summary>
    ///     Sets the currently selected entity ID.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the entity ID to select.</param>
    /// <returns>A new state with the entity ID set.</returns>
    public static EntitySelectionState SetEntityId(
        EntitySelectionState state,
        SetEntityIdAction action
    ) =>
        state with
        {
            EntityId = string.IsNullOrEmpty(action.EntityId) ? null : action.EntityId,
        };
}
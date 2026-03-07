namespace Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Pure reducer functions for the DualEntitySelection feature state.
/// </summary>
internal static class DualEntitySelectionReducers
{
    /// <summary>
    ///     Sets the account A entity ID.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the entity ID.</param>
    /// <returns>A new state with account A updated.</returns>
    public static DualEntitySelectionState SetEntityAId(
        DualEntitySelectionState state,
        SetEntityAIdAction action
    ) =>
        state with
        {
            AccountAId = string.IsNullOrEmpty(action.EntityId) ? null : action.EntityId,
        };

    /// <summary>
    ///     Sets the account B entity ID.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the entity ID.</param>
    /// <returns>A new state with account B updated.</returns>
    public static DualEntitySelectionState SetEntityBId(
        DualEntitySelectionState state,
        SetEntityBIdAction action
    ) =>
        state with
        {
            AccountBId = string.IsNullOrEmpty(action.EntityId) ? null : action.EntityId,
        };
}
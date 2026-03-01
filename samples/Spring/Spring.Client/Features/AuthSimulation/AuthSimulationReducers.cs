namespace Spring.Client.Features.AuthSimulation;

/// <summary>
///     Reducers for auth simulation feature state.
/// </summary>
internal static class AuthSimulationReducers
{
    /// <summary>
    ///     Sets the active auth simulation profile.
    /// </summary>
    /// <param name="state">Current feature state.</param>
    /// <param name="action">Profile action payload.</param>
    /// <returns>Updated feature state.</returns>
    public static AuthSimulationState SetProfile(
        AuthSimulationState state,
        SetAuthSimulationProfileAction action
    ) =>
        state with
        {
            Name = action.Name,
            Description = action.Description,
            IsAnonymous = action.IsAnonymous,
            Roles = action.Roles,
            Claims = action.Claims,
        };
}
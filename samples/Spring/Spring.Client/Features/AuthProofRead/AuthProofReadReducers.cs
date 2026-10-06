using MississippiSamples.Spring.Client.Features.AuthSimulation;


namespace MississippiSamples.Spring.Client.Features.AuthProofRead;

/// <summary>Accepts only the current protected read and invalidates observations when the persona changes.</summary>
internal static class AuthProofReadReducers
{
    /// <summary>Completes only the current in-flight read, including denied and empty outcomes.</summary>
    /// <param name="state">Current read state.</param>
    /// <param name="action">The correlated outcome.</param>
    /// <returns>The current state, or its completed observation.</returns>
    public static AuthProofReadState Complete(
        AuthProofReadState state,
        AuthProofProjectionReadCompletedAction action
    ) =>
        state.IsLoading && (state.RequestId == action.RequestId)
            ? state with
            {
                IsLoading = false,
                Data = action.Data,
                Version = action.Version,
                ErrorMessage = action.ErrorMessage,
            }
            : state;

    /// <summary>Invalidates a protected observation when a new persona is chosen.</summary>
    /// <param name="state">Current read state.</param>
    /// <param name="action">The selected profile.</param>
    /// <returns>An empty state with no prior request eligible to complete.</returns>
    public static AuthProofReadState InvalidatePersona(
        AuthProofReadState state,
        SetAuthSimulationProfileAction action
    )
    {
        _ = state;
        _ = action;
        return new();
    }

    /// <summary>Starts a protected read and clears the previous entity/persona observation.</summary>
    /// <param name="state">Current read state.</param>
    /// <param name="action">The requested entity and immutable persona.</param>
    /// <returns>The pending read state.</returns>
    public static AuthProofReadState Request(
        AuthProofReadState state,
        ReadAuthProofProjectionAction action
    )
    {
        _ = state;
        return new()
        {
            RequestId = action.RequestId,
            EntityId = action.EntityId,
            PersonaName = action.Persona.Name,
            IsLoading = true,
        };
    }
}
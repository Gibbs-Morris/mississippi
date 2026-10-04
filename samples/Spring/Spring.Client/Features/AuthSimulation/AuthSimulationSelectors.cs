using System;


namespace MississippiSamples.Spring.Client.Features.AuthSimulation;

/// <summary>
///     Selectors for deriving values from <see cref="AuthSimulationState" />.
/// </summary>
internal static class AuthSimulationSelectors
{
    /// <summary>
    ///     Gets the active persona description.
    /// </summary>
    /// <param name="state">Auth simulation state.</param>
    /// <returns>Persona description.</returns>
    public static string GetDescription(
        AuthSimulationState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Description;
    }

    /// <summary>
    ///     Gets a value indicating whether requests should be anonymous.
    /// </summary>
    /// <param name="state">Auth simulation state.</param>
    /// <returns><c>true</c> when anonymous; otherwise <c>false</c>.</returns>
    public static bool GetIsAnonymous(
        AuthSimulationState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.IsAnonymous;
    }

    /// <summary>
    ///     Gets the active persona name.
    /// </summary>
    /// <param name="state">Auth simulation state.</param>
    /// <returns>Persona name.</returns>
    public static string GetName(
        AuthSimulationState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Name;
    }
}
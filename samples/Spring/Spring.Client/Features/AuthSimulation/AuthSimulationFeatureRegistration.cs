using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;

using MississippiSamples.Spring.Client.AuthSimulation;


namespace MississippiSamples.Spring.Client.Features.AuthSimulation;

/// <summary>
///     Extension methods for registering auth simulation feature.
/// </summary>
internal static class AuthSimulationFeatureRegistration
{
    /// <summary>
    ///     Adds the auth simulation feature to the service collection.
    /// </summary>
    /// <param name="reservoir">Reservoir builder.</param>
    /// <returns>Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddAuthSimulationFeature(
        this IReservoirBuilder reservoir
    )
    {
        reservoir.AddFeature<AuthSimulationState>(feature => feature
            .AddReducer<AuthSimulationState, SetAuthSimulationProfileAction>(AuthSimulationReducers.SetProfile)
            .AddActionEffect<AuthSimulationState, AuthSimulationSyncEffect>());
        return reservoir;
    }
}
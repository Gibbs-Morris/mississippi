using Mississippi.Reservoir.Abstractions;

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
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IReservoirBuilder AddAuthSimulationFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeatureState<AuthSimulationState>(feature => feature
            .AddReducer<SetAuthSimulationProfileAction>(AuthSimulationReducers.SetProfile)
            .AddActionEffect<AuthSimulationSyncEffect>());
        return builder;
    }
}
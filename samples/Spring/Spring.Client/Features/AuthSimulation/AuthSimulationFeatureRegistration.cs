using Mississippi.Reservoir.Core;

using Spring.Client.AuthSimulation;


namespace Spring.Client.Features.AuthSimulation;

/// <summary>
///     Extension methods for registering auth simulation feature.
/// </summary>
internal static class AuthSimulationFeatureRegistration
{
    /// <summary>
    ///     Adds the auth simulation feature to the Reservoir store.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The same Reservoir builder for fluent chaining.</returns>
    public static IReservoirBuilder AddAuthSimulationFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeature<AuthSimulationState>(feature =>
        {
            feature.AddReducer<SetAuthSimulationProfileAction, AuthSimulationState>(AuthSimulationReducers.SetProfile);
            feature.AddActionEffect<AuthSimulationSyncEffect, AuthSimulationState>();
        });
        return builder;
    }
}
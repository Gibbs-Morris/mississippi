using Mississippi.Reservoir.Abstractions;

using MississippiSamples.Spring.Client.Features.AuthSimulation;


namespace MississippiSamples.Spring.Client.Features.AuthProofRead;

/// <summary>Registers the sample's persona-correlated protected HTTP read.</summary>
internal static class AuthProofReadFeatureRegistration
{
    /// <summary>Adds protected-read reducers and the registered Inlet fetcher's action effect.</summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IReservoirBuilder AddAuthProofReadFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeatureState<AuthProofReadState>(feature => feature
            .AddReducer<ReadAuthProofProjectionAction>(AuthProofReadReducers.Request)
            .AddReducer<AuthProofProjectionReadCompletedAction>(AuthProofReadReducers.Complete)
            .AddReducer<SetAuthSimulationProfileAction>(AuthProofReadReducers.InvalidatePersona)
            .AddActionEffect<AuthProofReadEffect>());
        return builder;
    }
}
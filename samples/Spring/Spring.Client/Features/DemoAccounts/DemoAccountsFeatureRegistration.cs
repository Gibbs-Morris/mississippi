using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace MississippiSamples.Spring.Client.Features.DemoAccounts;

/// <summary>
///     Extension methods for registering the demo accounts feature.
/// </summary>
internal static class DemoAccountsFeatureRegistration
{
    /// <summary>
    ///     Adds the demo accounts feature to the service collection.
    /// </summary>
    /// <param name="reservoir">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddDemoAccountsFeature(
        this IReservoirBuilder reservoir
    )
    {
        reservoir.AddFeature<DemoAccountsState>(feature =>
            feature.AddReducer<DemoAccountsState, SetDemoAccountsAction>(DemoAccountsReducers.SetDemoAccounts));
        return reservoir;
    }
}
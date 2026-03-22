using Mississippi.Reservoir.Abstractions;


namespace MississippiSamples.Spring.Client.Features.DemoAccounts;

/// <summary>
///     Extension methods for registering the demo accounts feature.
/// </summary>
internal static class DemoAccountsFeatureRegistration
{
    /// <summary>
    ///     Adds the demo accounts feature to the service collection.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IReservoirBuilder AddDemoAccountsFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeatureState<DemoAccountsState>(feature =>
            feature.AddReducer<SetDemoAccountsAction>(DemoAccountsReducers.SetDemoAccounts));
        return builder;
    }
}
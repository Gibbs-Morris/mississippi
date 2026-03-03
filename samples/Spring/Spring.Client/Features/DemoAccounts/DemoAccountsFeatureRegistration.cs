using Mississippi.Reservoir.Core;


namespace Spring.Client.Features.DemoAccounts;

/// <summary>
///     Extension methods for registering the demo accounts feature.
/// </summary>
internal static class DemoAccountsFeatureRegistration
{
    /// <summary>
    ///     Adds the demo accounts feature to the Reservoir store.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The same Reservoir builder for fluent chaining.</returns>
    public static IReservoirBuilder AddDemoAccountsFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeature<DemoAccountsState>(feature =>
        {
            feature.AddReducer<SetDemoAccountsAction, DemoAccountsState>(DemoAccountsReducers.SetDemoAccounts);
        });
        return builder;
    }
}
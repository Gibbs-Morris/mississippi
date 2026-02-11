using System;

using Mississippi.Reservoir.Abstractions.Builders;


namespace Spring.Client.Features.DemoAccounts;

/// <summary>
///     Extension methods for registering the demo accounts feature.
/// </summary>
internal static class DemoAccountsFeatureRegistration
{
    /// <summary>
    ///     Adds the demo accounts feature to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddDemoAccountsFeature(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFeature<DemoAccountsState>(featureBuilder =>
        {
            featureBuilder.AddReducer<SetDemoAccountsAction>(DemoAccountsReducers.SetDemoAccounts);
        });
        return builder;
    }
}
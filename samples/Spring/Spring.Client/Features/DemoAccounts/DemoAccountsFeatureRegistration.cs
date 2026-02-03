using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace Spring.Client.Features.DemoAccounts;

/// <summary>
///     Extension methods for registering the demo accounts feature.
/// </summary>
internal static class DemoAccountsFeatureRegistration
{
    /// <summary>
    ///     Adds the demo accounts feature to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDemoAccountsFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetDemoAccountsAction, DemoAccountsState>(DemoAccountsReducers.SetDemoAccounts);
        return services;
    }
}
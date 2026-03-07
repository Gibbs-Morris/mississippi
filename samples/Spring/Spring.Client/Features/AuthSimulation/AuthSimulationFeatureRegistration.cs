using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Core;
using Mississippi.Spring.Client.AuthSimulation;


namespace Mississippi.Spring.Client.Features.AuthSimulation;

/// <summary>
///     Extension methods for registering auth simulation feature.
/// </summary>
internal static class AuthSimulationFeatureRegistration
{
    /// <summary>
    ///     Adds the auth simulation feature to the service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddAuthSimulationFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetAuthSimulationProfileAction, AuthSimulationState>(AuthSimulationReducers.SetProfile);
        services.AddActionEffect<AuthSimulationState, AuthSimulationSyncEffect>();
        return services;
    }
}
#pragma warning disable CS0618 // Sample still exercises legacy composition APIs pending issue #237.
using Microsoft.Extensions.DependencyInjection;

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

#pragma warning restore CS0618
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Provides extension methods for registering Reservoir components in the dependency injection container.
/// </summary>
public static class ReservoirRegistrations
{
    /// <summary>
    ///     Adds the Store to the service collection with DI-resolved feature states and middleware.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <remarks>
    ///     All effects are feature-scoped via <see cref="IActionEffect{TState}" /> and are
    ///     resolved through feature state registrations. There are no global effects.
    /// </remarks>
    public static IReservoirBuilder AddReservoir(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IStore>(sp => new Store(
            sp.GetServices<IFeatureStateRegistration>(),
            sp.GetServices<IMiddleware>(),
            sp.GetRequiredService<TimeProvider>()));
        return new ReservoirBuilder(services);
    }
}
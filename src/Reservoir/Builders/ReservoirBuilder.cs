using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Common.Abstractions.Builders;
using Mississippi.Reservoir.Abstractions.Builders;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;
using Mississippi.Reservoir.State;


namespace Mississippi.Reservoir.Builders;

/// <summary>
///     Builder for Reservoir registration.
/// </summary>
public sealed class ReservoirBuilder : IReservoirBuilder
{
    private readonly IMississippiClientBuilder parent;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public ReservoirBuilder(
        IMississippiClientBuilder parent
    )
    {
        ArgumentNullException.ThrowIfNull(parent);
        this.parent = parent;
        AddStore();
    }

    /// <inheritdoc />
    public IReservoirBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        parent.ConfigureServices(configure);
        return this;
    }

    /// <inheritdoc />
    public IReservoirBuilder AddMiddleware<TMiddleware>()
        where TMiddleware : class, IMiddleware
    {
        ConfigureServices(services => services.AddTransient<IMiddleware, TMiddleware>());
        return this;
    }

    /// <inheritdoc />
    public IReservoirFeatureBuilder<TState> AddFeature<TState>()
        where TState : class, IFeatureState, new()
    {
        AddFeatureState<TState>();
        return new ReservoirFeatureBuilder<TState>(this);
    }

    private void AddStore()
    {
        ConfigureServices(services =>
        {
            services.TryAddSingleton(TimeProvider.System);
            services.TryAddScoped<IStore>(sp => new Store(
                sp.GetServices<IFeatureStateRegistration>(),
                sp.GetServices<IMiddleware>(),
                sp.GetRequiredService<TimeProvider>()));
        });
    }

    private void AddFeatureState<TState>()
        where TState : class, IFeatureState, new()
    {
        ConfigureServices(services =>
            services.TryAddEnumerable(
                ServiceDescriptor.Scoped<IFeatureStateRegistration, FeatureStateRegistration<TState>>(sp => new(
                    sp.GetService<IRootReducer<TState>>(),
                    sp.GetService<IRootActionEffect<TState>>()))));
    }
}

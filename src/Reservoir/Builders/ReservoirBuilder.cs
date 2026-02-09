using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;
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
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public ReservoirBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
        AddStore();
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IReservoirBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }

    /// <inheritdoc />
    public IReservoirBuilder AddMiddleware<TMiddleware>()
        where TMiddleware : class, IMiddleware
    {
        Services.AddTransient<IMiddleware, TMiddleware>();
        return this;
    }

    /// <inheritdoc />
    public IReservoirFeatureBuilder<TState> AddFeature<TState>()
        where TState : class, IFeatureState, new()
    {
        AddFeatureState<TState>();
        return new ReservoirFeatureBuilder<TState>(Services, this);
    }

    private void AddStore()
    {
        Services.TryAddSingleton(TimeProvider.System);
        Services.TryAddScoped<IStore>(sp => new Store(
            sp.GetServices<IFeatureStateRegistration>(),
            sp.GetServices<IMiddleware>(),
            sp.GetRequiredService<TimeProvider>()));
    }

    private void AddFeatureState<TState>()
        where TState : class, IFeatureState, new()
    {
        Services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IFeatureStateRegistration, FeatureStateRegistration<TState>>(sp => new(
                sp.GetService<IRootReducer<TState>>(),
                sp.GetService<IRootActionEffect<TState>>())));
    }
}

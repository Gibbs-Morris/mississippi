using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Builders;
using Mississippi.Reservoir.Abstractions.State;
using Mississippi.Reservoir.State;


namespace Mississippi.Reservoir.Builders;

/// <summary>
///     Builder for Reservoir registration.
/// </summary>
public sealed class ReservoirBuilder : IReservoirBuilder
{
    private IMississippiClientBuilder Parent { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBuilder" /> class.
    /// </summary>
    /// <param name="parent">The parent builder.</param>
    public ReservoirBuilder(
        IMississippiClientBuilder parent
    )
    {
        ArgumentNullException.ThrowIfNull(parent);
        Parent = parent;
        AddStore();
    }

    /// <inheritdoc />
    public IReservoirBuilder AddFeature<TState>(
        Action<IReservoirFeatureBuilder<TState>> configure
    )
        where TState : class, IFeatureState, new()
    {
        ArgumentNullException.ThrowIfNull(configure);
        AddFeatureState<TState>();
        configure(new ReservoirFeatureBuilder<TState>(this));
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
    public IReservoirBuilder ConfigureOptions<TOptions>(
        Action<TOptions> configure
    )
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        Parent.ConfigureOptions(configure);
        return this;
    }

    /// <inheritdoc />
    public IReservoirBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        Parent.ConfigureServices(configure);
        return this;
    }

    private void AddFeatureState<TState>()
        where TState : class, IFeatureState, new()
    {
        ConfigureServices(services => services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IFeatureStateRegistration, FeatureStateRegistration<TState>>(sp => new(
                sp.GetService<IRootReducer<TState>>(),
                sp.GetService<IRootActionEffect<TState>>()))));
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
}
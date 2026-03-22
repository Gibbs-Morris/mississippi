using System;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Implements the top-level Reservoir builder over an <see cref="IServiceCollection" />.
/// </summary>
internal sealed class ReservoirBuilder : IReservoirBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    public ReservoirBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    ///     Gets the underlying service collection for advanced extension scenarios.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IServiceCollection Services { get; }

    /// <summary>
    ///     Adds a feature state without additional feature configuration.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public IReservoirBuilder AddFeatureState<TState>()
        where TState : class, IFeatureState, new()
    {
        ReservoirBuilderRegistrations.AddFeatureState<TState>(Services);
        return this;
    }

    /// <summary>
    ///     Adds a feature state and configures its reducers and action effects.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="configure">The callback used to configure the feature builder.</param>
    /// <returns>The builder for chaining.</returns>
    public IReservoirBuilder AddFeatureState<TState>(
        Action<IReservoirFeatureBuilder<TState>> configure
    )
        where TState : class, IFeatureState, new()
    {
        ArgumentNullException.ThrowIfNull(configure);
        AddFeatureStateTransactionally(configure);
        return this;
    }

    /// <summary>
    ///     Adds a middleware implementation to the Reservoir pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware implementation type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public IReservoirBuilder AddMiddleware<TMiddleware>()
        where TMiddleware : class, IMiddleware
    {
        ReservoirBuilderRegistrations.AddMiddleware<TMiddleware>(Services);
        return this;
    }

    private void AddFeatureStateTransactionally<TState>(
        Action<IReservoirFeatureBuilder<TState>> configure
    )
        where TState : class, IFeatureState, new()
    {
        IServiceCollection stagedServices = new ServiceCollection();
        foreach (ServiceDescriptor descriptor in Services)
        {
            stagedServices.Add(descriptor);
        }

        ReservoirBuilderRegistrations.AddFeatureState<TState>(stagedServices);
        configure(new ReservoirFeatureBuilder<TState>(stagedServices));
        Services.Clear();
        foreach (ServiceDescriptor descriptor in stagedServices)
        {
            Services.Add(descriptor);
        }
    }
}
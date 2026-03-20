using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Default Reservoir builder implementation over an <see cref="IServiceCollection" />.
/// </summary>
public sealed class ReservoirBuilder : IReservoirBuilder
{
    private readonly HashSet<Type> configuredFeatures = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection backing this builder.</param>
    public ReservoirBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
        ReservoirServiceRegistrationHelpers.AddReservoirCoreServices(services);
    }

    /// <summary>
    ///     Gets the backing service collection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IReservoirBuilder AddFeature<TState>(
        Action<IFeatureStateBuilder<TState>> configure
    )
        where TState : class, IFeatureState, new()
    {
        ArgumentNullException.ThrowIfNull(configure);
        RegisterFeature<TState>();
        configure(new ReservoirFeatureStateBuilder<TState>(this));
        return this;
    }

    /// <inheritdoc />
    public IReservoirBuilder AddFeature<TState>()
        where TState : class, IFeatureState, new()
    {
        RegisterFeature<TState>();
        return this;
    }

    /// <inheritdoc />
    public void Validate()
    {
        if (configuredFeatures.Count == 0)
        {
            throw new ReservoirBuilderValidationException(
                "Reservoir requires at least one configured feature. Add a feature before attaching Reservoir to the host.");
        }
    }

    private void RegisterFeature<TState>()
        where TState : class, IFeatureState, new()
    {
        if (!configuredFeatures.Add(typeof(TState)))
        {
            throw new ReservoirBuilderValidationException(
                $"Reservoir feature '{typeof(TState).FullName}' was configured more than once.");
        }

        ReservoirServiceRegistrationHelpers.AddFeatureState<TState>(Services);
    }
}
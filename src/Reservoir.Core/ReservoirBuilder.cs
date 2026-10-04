using System;
using System.ComponentModel;
using System.Linq;

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
        ParentServices = services;
    }

    /// <summary>
    ///     Gets the underlying service collection for advanced extension scenarios.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IServiceCollection Services
    {
        get
        {
            ThrowIfConfiguringFeature();
            return ParentServices;
        }
    }

    private bool IsConfiguringFeature { get; set; }

    private IServiceCollection ParentServices { get; }

    /// <summary>
    ///     Adds a feature state without additional feature configuration.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public IReservoirBuilder AddFeatureState<TState>()
        where TState : class, IFeatureState, new()
    {
        ThrowIfConfigurationUnavailable();
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
        ThrowIfConfigurationUnavailable();
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
        ThrowIfConfigurationUnavailable();
        ReservoirBuilderRegistrations.AddMiddleware<TMiddleware>(Services);
        return this;
    }

    private void AddFeatureStateTransactionally<TState>(
        Action<IReservoirFeatureBuilder<TState>> configure
    )
        where TState : class, IFeatureState, new()
    {
        ServiceDescriptor[] originalServices = Services.ToArray();
        ServiceCollection stagedServices = [];
        foreach (ServiceDescriptor descriptor in originalServices)
        {
            ((IServiceCollection)stagedServices).Add(descriptor);
        }

        try
        {
            ReservoirBuilderRegistrations.AddFeatureState<TState>(stagedServices);
            IsConfiguringFeature = true;
            try
            {
                configure(new ReservoirFeatureBuilder<TState>(stagedServices));
            }
            finally
            {
                IsConfiguringFeature = false;
            }

            ThrowIfConfigurationUnavailable();
            if (!Services.SequenceEqual(originalServices))
            {
                throw new InvalidOperationException(
                    "Parent Reservoir services changed during a feature callback. Use the supplied feature builder, or configure parent services outside that callback.");
            }

            Services.Clear();
            foreach (ServiceDescriptor descriptor in stagedServices)
            {
                Services.Add(descriptor);
            }
        }
        finally
        {
            stagedServices.MakeReadOnly();
        }
    }

    private void ThrowIfConfigurationUnavailable()
    {
        ThrowIfConfiguringFeature();
        if (ParentServices.IsReadOnly)
        {
            throw new InvalidOperationException(
                "Reservoir services are read-only. Configure features while the owning configuration scope is open.");
        }
    }

    private void ThrowIfConfiguringFeature()
    {
        if (IsConfiguringFeature)
        {
            throw new InvalidOperationException(
                "Reservoir root configuration is unavailable inside a feature callback. Use the supplied feature builder; register other states and middleware outside that callback.");
        }
    }
}
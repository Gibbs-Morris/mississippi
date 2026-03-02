using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Runtime.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Common.Builders.Runtime;

/// <summary>
///     Concrete runtime-host builder implementation.
/// </summary>
public sealed class RuntimeBuilder : IRuntimeBuilder
{
    private RuntimeBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <summary>
    ///     Gets a value indicating whether runtime silo configuration was applied.
    /// </summary>
    public bool SiloConfigurationApplied { get; private set; }

    /// <summary>
    ///     Creates a new runtime builder instance.
    /// </summary>
    /// <returns>A new <see cref="RuntimeBuilder" /> instance.</returns>
    public static RuntimeBuilder Create() => new(new ServiceCollection());

    /// <inheritdoc />
    public IAggregateBuilder<TSnapshot> AddAggregate<TSnapshot>() => new AggregateBuilder<TSnapshot>(Services);

    /// <inheritdoc />
    public IFeatureStateBuilder<TFeatureState> AddFeatureState<TFeatureState>() =>
        new FeatureStateBuilder<TFeatureState>(Services);

    /// <inheritdoc />
    public ISagaBuilder<TSagaState> AddSaga<TSagaState>() => new SagaBuilder<TSagaState>(Services);

    /// <inheritdoc />
    public IUxProjectionBuilder<TProjectionState> AddUxProjection<TProjectionState>() =>
        new UxProjectionBuilder<TProjectionState>(Services);

    /// <inheritdoc />
    public IRuntimeBuilder ApplyToSilo(
        ISiloBuilder siloBuilder
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        SiloConfigurationApplied = true;
        return this;
    }

    /// <inheritdoc />
    public IRuntimeBuilder ConfigureSnapshotRetention(
        Action<SnapshotRetentionOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(configure);
        return this;
    }
}
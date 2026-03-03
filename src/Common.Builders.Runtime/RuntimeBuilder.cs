using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Common.Builders.Runtime.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Common.Builders.Runtime;

/// <summary>
///     Concrete runtime-host builder implementation.
/// </summary>
public sealed class RuntimeBuilder : IRuntimeBuilder
{
    /// <summary>
    ///     Feature marker key for Brooks runtime infrastructure.
    /// </summary>
    internal const string BrooksFeatureName = "Runtime.Brooks";

    /// <summary>
    ///     Feature marker key for domain-modeling runtime infrastructure.
    /// </summary>
    internal const string DomainModelingFeatureName = "Runtime.DomainModeling";

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

    private HashSet<string> ConfiguredFeatures { get; } = [];

    private List<Action<ISiloBuilder>> SiloActions { get; } = [];

    /// <summary>
    ///     Creates a new runtime builder instance.
    /// </summary>
    /// <returns>A new <see cref="RuntimeBuilder" /> instance.</returns>
    public static RuntimeBuilder Create() => new(new ServiceCollection());

    /// <inheritdoc />
    public IAggregateBuilder<TSnapshot> AddAggregate<TSnapshot>() => new AggregateBuilder<TSnapshot>(Services);

    /// <inheritdoc />
    public ISagaBuilder<TSagaState> AddSaga<TSagaState>() => new SagaBuilder<TSagaState>(Services);

    /// <summary>
    ///     Adds runtime-specific Orleans silo configuration that will be replayed during <see cref="ApplyToSilo" />.
    /// </summary>
    /// <param name="configure">Silo configuration delegate.</param>
    /// <returns>The same runtime builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure" /> is null.</exception>
    public IRuntimeBuilder AddSiloConfiguration(
        Action<ISiloBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        SiloActions.Add(configure);
        return this;
    }

    /// <inheritdoc />
    public IUxProjectionBuilder<TProjectionState> AddUxProjection<TProjectionState>() =>
        new UxProjectionBuilder<TProjectionState>(Services);

    /// <inheritdoc />
    public IRuntimeBuilder ApplyToSilo(
        ISiloBuilder siloBuilder
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        foreach (Action<ISiloBuilder> siloAction in SiloActions)
        {
            siloAction(siloBuilder);
        }

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

    /// <inheritdoc />
    public bool IsFeatureConfigured(
        string featureName
    )
    {
        ArgumentNullException.ThrowIfNull(featureName);
        return ConfiguredFeatures.Contains(featureName);
    }

    /// <inheritdoc />
    public IRuntimeBuilder MarkFeatureConfigured(
        string featureName
    )
    {
        ArgumentNullException.ThrowIfNull(featureName);
        ConfiguredFeatures.Add(featureName);
        return this;
    }

    /// <inheritdoc />
    public IReadOnlyList<BuilderDiagnostic> Validate()
    {
        List<BuilderDiagnostic> diagnostics = [];
        if (!IsFeatureConfigured(BrooksFeatureName))
        {
            diagnostics.Add(
                new(
                    BrooksFeatureName + "NotConfigured",
                    "Configuration",
                    "Runtime Brooks infrastructure is not configured.",
                    "Call runtime.AddBrooks(...) before terminal host attachment."));
        }

        if (!IsFeatureConfigured(DomainModelingFeatureName))
        {
            diagnostics.Add(
                new(
                    DomainModelingFeatureName + "NotConfigured",
                    "Configuration",
                    "Runtime domain modeling infrastructure is not configured.",
                    "Call runtime.AddDomainModeling() before terminal host attachment."));
        }

        return diagnostics;
    }
}
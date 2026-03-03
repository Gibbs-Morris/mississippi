using System;

using Mississippi.Common.Builders.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Common.Builders.Runtime.Abstractions;

/// <summary>
///     Contract for configuring Mississippi runtime host composition.
/// </summary>
public interface IRuntimeBuilder : IMississippiBuilder
{
    /// <summary>
    ///     Starts typed aggregate composition.
    /// </summary>
    /// <typeparam name="TSnapshot">Aggregate snapshot type.</typeparam>
    /// <returns>Aggregate composition builder.</returns>
    IAggregateBuilder<TSnapshot> AddAggregate<TSnapshot>();

    /// <summary>
    ///     Starts typed saga composition.
    /// </summary>
    /// <typeparam name="TSagaState">Saga state type.</typeparam>
    /// <returns>Saga composition builder.</returns>
    ISagaBuilder<TSagaState> AddSaga<TSagaState>();

    /// <summary>
    ///     Adds runtime-specific Orleans silo configuration that will be replayed during <see cref="ApplyToSilo" />.
    /// </summary>
    /// <param name="configure">Silo configuration delegate.</param>
    /// <returns>The same runtime builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure" /> is null.</exception>
    IRuntimeBuilder AddSiloConfiguration(
        Action<ISiloBuilder> configure
    );

    /// <summary>
    ///     Starts typed UX projection composition.
    /// </summary>
    /// <typeparam name="TProjectionState">Projection state type.</typeparam>
    /// <returns>Projection composition builder.</returns>
    IUxProjectionBuilder<TProjectionState> AddUxProjection<TProjectionState>();

    /// <summary>
    ///     Applies runtime-specific configuration to the Orleans silo builder.
    /// </summary>
    /// <param name="siloBuilder">Silo builder being configured.</param>
    /// <returns>The same builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="siloBuilder" /> is null.</exception>
    IRuntimeBuilder ApplyToSilo(
        ISiloBuilder siloBuilder
    );

    /// <summary>
    ///     Configures snapshot retention policy.
    /// </summary>
    /// <param name="configure">Snapshot retention configuration delegate.</param>
    /// <returns>The same builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure" /> is null.</exception>
    IRuntimeBuilder ConfigureSnapshotRetention(
        Action<SnapshotRetentionOptions> configure
    );

    /// <summary>
    ///     Gets whether a named runtime feature has been configured.
    /// </summary>
    /// <param name="featureName">Feature key.</param>
    /// <returns><c>true</c> when configured; otherwise <c>false</c>.</returns>
    bool IsFeatureConfigured(
        string featureName
    );

    /// <summary>
    ///     Marks a named runtime feature as configured for validation.
    /// </summary>
    /// <param name="featureName">Feature key.</param>
    /// <returns>The same runtime builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="featureName" /> is null.</exception>
    IRuntimeBuilder MarkFeatureConfigured(
        string featureName
    );
}
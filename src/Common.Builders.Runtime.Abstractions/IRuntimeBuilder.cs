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
    ///     Starts typed feature-state composition.
    /// </summary>
    /// <typeparam name="TFeatureState">Feature state type.</typeparam>
    /// <returns>Feature-state composition builder.</returns>
    IFeatureStateBuilder<TFeatureState> AddFeatureState<TFeatureState>();

    /// <summary>
    ///     Starts typed saga composition.
    /// </summary>
    /// <typeparam name="TSagaState">Saga state type.</typeparam>
    /// <returns>Saga composition builder.</returns>
    ISagaBuilder<TSagaState> AddSaga<TSagaState>();

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
}
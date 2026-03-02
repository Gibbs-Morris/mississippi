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
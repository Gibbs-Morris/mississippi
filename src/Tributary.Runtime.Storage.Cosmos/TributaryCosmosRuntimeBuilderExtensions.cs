using System;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos;

/// <summary>
///     Tributary runtime sub-builder extension methods for Cosmos snapshot storage configuration.
/// </summary>
public static class TributaryCosmosRuntimeBuilderExtensions
{
    /// <summary>
    ///     Adds Cosmos-backed snapshot storage.
    /// </summary>
    /// <param name="builder">Tributary runtime sub-builder.</param>
    /// <param name="configure">Storage options configuration.</param>
    /// <returns>The same Tributary runtime sub-builder for fluent chaining.</returns>
    public static ITributaryRuntimeBuilder UseCosmosSnapshotStorage(
        this ITributaryRuntimeBuilder builder,
        Action<SnapshotStorageOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        builder.Services.AddCosmosSnapshotStorageProvider(configure);
        return builder;
    }
}
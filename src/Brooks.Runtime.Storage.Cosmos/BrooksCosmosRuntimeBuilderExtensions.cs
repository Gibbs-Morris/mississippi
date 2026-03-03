using System;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos;

/// <summary>
///     Brooks runtime sub-builder extension methods for Cosmos storage configuration.
/// </summary>
public static class BrooksCosmosRuntimeBuilderExtensions
{
    /// <summary>
    ///     Adds Cosmos-backed Brooks event-stream storage.
    /// </summary>
    /// <param name="builder">Brooks runtime sub-builder.</param>
    /// <param name="configure">Storage options configuration.</param>
    /// <returns>The same Brooks runtime sub-builder for fluent chaining.</returns>
    public static IBrooksRuntimeBuilder UseCosmosStorage(
        this IBrooksRuntimeBuilder builder,
        Action<BrookStorageOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        builder.Services.AddCosmosBrookStorageProvider(configure);
        return builder;
    }
}
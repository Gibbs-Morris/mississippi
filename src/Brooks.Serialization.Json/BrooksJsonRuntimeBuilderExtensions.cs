using System;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Serialization.Json;

/// <summary>
///     Brooks runtime sub-builder extension methods for JSON serialization.
/// </summary>
public static class BrooksJsonRuntimeBuilderExtensions
{
    /// <summary>
    ///     Adds JSON serialization for Brooks runtime operations.
    /// </summary>
    /// <param name="builder">Brooks runtime sub-builder.</param>
    /// <returns>The same Brooks runtime sub-builder for fluent chaining.</returns>
    public static IBrooksRuntimeBuilder UseJsonSerialization(
        this IBrooksRuntimeBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddJsonSerialization();
        return builder;
    }
}
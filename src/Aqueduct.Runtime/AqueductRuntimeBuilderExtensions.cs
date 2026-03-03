using System;

using Mississippi.Common.Builders.Runtime.Abstractions;


namespace Mississippi.Aqueduct.Runtime;

/// <summary>
///     Runtime-builder extension methods for Aqueduct registration.
/// </summary>
public static class AqueductRuntimeBuilderExtensions
{
    /// <summary>
    ///     Adds Aqueduct silo configuration through the runtime builder.
    /// </summary>
    /// <param name="builder">Runtime builder.</param>
    /// <param name="configureOptions">Aqueduct configuration delegate.</param>
    /// <returns>The same runtime builder instance for fluent chaining.</returns>
    public static IRuntimeBuilder AddAqueduct(
        this IRuntimeBuilder builder,
        Action<AqueductSiloOptions> configureOptions
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);
        builder.AddSiloConfiguration(siloBuilder => siloBuilder.UseAqueduct(configureOptions));
        return builder;
    }
}
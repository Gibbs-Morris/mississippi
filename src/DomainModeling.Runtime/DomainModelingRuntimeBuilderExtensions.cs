using System;

using Mississippi.Common.Builders.Runtime.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Runtime-builder extension methods for DomainModeling infrastructure registration.
/// </summary>
public static class DomainModelingRuntimeBuilderExtensions
{
    /// <summary>
    ///     Adds aggregate and projection runtime infrastructure.
    /// </summary>
    /// <param name="builder">Runtime builder.</param>
    /// <returns>The same runtime builder instance for fluent chaining.</returns>
    public static IRuntimeBuilder AddDomainModeling(
        this IRuntimeBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddAggregateSupport();
        builder.Services.AddUxProjections();
        builder.MarkFeatureConfigured("Runtime.DomainModeling");
        return builder;
    }
}
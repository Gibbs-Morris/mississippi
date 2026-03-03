using System;

using Mississippi.Brooks.Serialization.Json;
using Mississippi.Common.Builders.Gateway.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Gateway-builder extension methods for DomainModeling infrastructure registration.
/// </summary>
public static class DomainModelingGatewayBuilderExtensions
{
    /// <summary>
    ///     Adds aggregate, projection, and serialization gateway infrastructure.
    /// </summary>
    /// <param name="builder">Gateway builder.</param>
    /// <returns>The same gateway builder instance for fluent chaining.</returns>
    public static IGatewayBuilder AddDomainModeling(
        this IGatewayBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddJsonSerialization();
        builder.Services.AddAggregateSupport();
        builder.Services.AddUxProjections();
        return builder;
    }
}
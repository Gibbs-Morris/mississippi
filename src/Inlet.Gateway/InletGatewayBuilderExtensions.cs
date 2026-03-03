using System;
using System.Reflection;

using Mississippi.Common.Builders.Gateway.Abstractions;
using Mississippi.Inlet.Runtime;


namespace Mississippi.Inlet.Gateway;

/// <summary>
///     Gateway-builder extension methods for Inlet registration.
/// </summary>
public static class InletGatewayBuilderExtensions
{
    /// <summary>
    ///     Adds Inlet server services and optionally scans projection assemblies.
    /// </summary>
    /// <param name="builder">Gateway builder.</param>
    /// <param name="configure">Optional Inlet server options configuration.</param>
    /// <param name="projectionAssemblies">Optional projection assemblies to scan.</param>
    /// <returns>The same gateway builder instance for fluent chaining.</returns>
    public static IGatewayBuilder AddInlet(
        this IGatewayBuilder builder,
        Action<InletServerOptions>? configure = null,
        params Assembly[] projectionAssemblies
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(projectionAssemblies);
        builder.Services.AddInletServer(configure);
        if (projectionAssemblies.Length > 0)
        {
            builder.Services.ScanProjectionAssemblies(projectionAssemblies);
        }

        return builder;
    }
}
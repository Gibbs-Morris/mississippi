using System;
using System.Reflection;

using Mississippi.Common.Builders.Runtime.Abstractions;


namespace Mississippi.Inlet.Runtime;

/// <summary>
///     Runtime-builder extension methods for Inlet registration.
/// </summary>
public static class InletRuntimeBuilderExtensions
{
    /// <summary>
    ///     Adds Inlet runtime services and optionally scans projection assemblies.
    /// </summary>
    /// <param name="builder">Runtime builder.</param>
    /// <param name="projectionAssemblies">Optional projection assemblies to scan.</param>
    /// <returns>The same runtime builder instance for fluent chaining.</returns>
    public static IRuntimeBuilder AddInlet(
        this IRuntimeBuilder builder,
        params Assembly[] projectionAssemblies
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(projectionAssemblies);
        builder.Services.AddInletSilo();
        if (projectionAssemblies.Length > 0)
        {
            builder.Services.ScanProjectionAssemblies(projectionAssemblies);
        }

        return builder;
    }
}
using System;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace Mississippi.Reservoir.Client;

/// <summary>
///     Extension methods for registering Reservoir from a WebAssembly host builder.
/// </summary>
/// <remarks>
///     Justification: public to provide the primary Blazor WebAssembly startup entry point.
/// </remarks>
public static class ReservoirWebAssemblyHostBuilderRegistrations
{
    /// <summary>
    ///     Adds Reservoir to a WebAssembly host builder.
    /// </summary>
    /// <param name="builder">The WebAssembly host builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddReservoir(
        this WebAssemblyHostBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Services.AddReservoir();
    }
}
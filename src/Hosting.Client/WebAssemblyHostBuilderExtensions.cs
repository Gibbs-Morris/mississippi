using System;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


namespace Mississippi.Hosting.Client;

/// <summary>
///     Extension methods for composing Mississippi client applications from a WebAssembly host builder.
/// </summary>
public static class WebAssemblyHostBuilderExtensions
{
    /// <summary>
    ///     Adds the top-level Mississippi client builder to a WebAssembly host builder.
    /// </summary>
    /// <param name="builder">The WebAssembly host builder.</param>
    /// <returns>The Mississippi client builder for further client composition.</returns>
    public static MississippiClientBuilder AddMississippiClient(
        this WebAssemblyHostBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new(builder);
    }

    /// <summary>
    ///     Adds the top-level Mississippi client builder to a WebAssembly host builder and invokes a configuration callback.
    /// </summary>
    /// <param name="builder">The WebAssembly host builder.</param>
    /// <param name="configure">The client composition callback.</param>
    /// <returns>The original WebAssembly host builder.</returns>
    public static WebAssemblyHostBuilder AddMississippiClient(
        this WebAssemblyHostBuilder builder,
        Action<MississippiClientBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        configure(builder.AddMississippiClient());
        return builder;
    }
}
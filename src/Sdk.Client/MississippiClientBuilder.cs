using System;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Sdk.Client;

/// <summary>
///     Fluent builder wrapper for Mississippi client registrations.
/// </summary>
public sealed class MississippiClientBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiClientBuilder" /> class.
    /// </summary>
    /// <param name="hostBuilder">The underlying WebAssembly host builder.</param>
    internal MississippiClientBuilder(
        WebAssemblyHostBuilder hostBuilder
    ) =>
        HostBuilder = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));

    /// <summary>
    ///     Gets or sets a value indicating whether domain registrations have been applied.
    /// </summary>
    public bool HasDomain { get; set; }

    /// <summary>
    ///     Gets the service collection for dependency injection.
    /// </summary>
    public IServiceCollection Services => HostBuilder.Services;

    /// <summary>
    ///     Gets the underlying WebAssembly host builder.
    /// </summary>
    internal WebAssemblyHostBuilder HostBuilder { get; }
}
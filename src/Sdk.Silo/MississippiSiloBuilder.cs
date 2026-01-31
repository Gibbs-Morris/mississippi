using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Orleans.Hosting;


namespace Mississippi.Sdk.Silo;

/// <summary>
///     Fluent builder wrapper for Mississippi silo registrations.
/// </summary>
public sealed class MississippiSiloBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiSiloBuilder" /> class.
    /// </summary>
    /// <param name="hostBuilder">The underlying host builder.</param>
    internal MississippiSiloBuilder(
        IHostApplicationBuilder hostBuilder
    ) =>
        HostBuilder = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));

    /// <summary>
    ///     Gets or sets a value indicating whether domain registrations have been applied.
    /// </summary>
    public bool HasDomain { get; set; }

    /// <summary>
    ///     Gets the underlying host builder.
    /// </summary>
    public IHostApplicationBuilder HostBuilder { get; }

    /// <summary>
    ///     Gets the service collection for dependency injection.
    /// </summary>
    public IServiceCollection Services => HostBuilder.Services;

    /// <summary>
    ///     Gets the list of Orleans configuration actions to apply at build time.
    /// </summary>
    internal List<Action<ISiloBuilder>> OrleansConfigurations { get; } = [];

    /// <summary>
    ///     Adds an Orleans configuration action for the underlying silo builder.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The Mississippi silo builder for chaining.</returns>
    public MississippiSiloBuilder UseOrleans(
        Action<ISiloBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        OrleansConfigurations.Add(configure);
        return this;
    }
}
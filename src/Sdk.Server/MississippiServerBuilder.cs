using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Sdk.Server;

/// <summary>
///     Fluent builder wrapper for Mississippi server registrations.
/// </summary>
public sealed class MississippiServerBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiServerBuilder" /> class.
    /// </summary>
    /// <param name="hostBuilder">The underlying web application builder.</param>
    internal MississippiServerBuilder(
        WebApplicationBuilder hostBuilder
    ) =>
        HostBuilder = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));

    /// <summary>
    ///     Gets the underlying web application builder.
    /// </summary>
    public WebApplicationBuilder HostBuilder { get; }

    /// <summary>
    ///     Gets the service collection for dependency injection.
    /// </summary>
    public IServiceCollection Services => HostBuilder.Services;

    /// <summary>
    ///     Gets the configuration manager.
    /// </summary>
    public ConfigurationManager Configuration => HostBuilder.Configuration;

    /// <summary>
    ///     Gets or sets a value indicating whether domain registrations have been applied.
    /// </summary>
    public bool HasDomain { get; set; }
}

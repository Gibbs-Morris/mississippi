using System;
using System.ComponentModel;
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Gateway;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Runtime;
using Mississippi.Inlet.Gateway;
using Mississippi.Inlet.Runtime;


namespace Mississippi.Hosting.Gateway;

/// <summary>
///     Provides the top-level gateway-role builder for Mississippi ASP.NET Core applications.
/// </summary>
public sealed class MississippiGatewayBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiGatewayBuilder" /> class.
    /// </summary>
    /// <param name="builder">The web application builder used for gateway startup.</param>
    internal MississippiGatewayBuilder(
        WebApplicationBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        Services = builder.Services;
    }

    /// <summary>
    ///     Gets the underlying service collection for advanced composition scenarios.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IServiceCollection Services { get; }

    private bool HasAqueductBeenAdded { get; set; }

    private bool HasInletGatewayBeenAdded { get; set; }

    private bool HasJsonSerializationBeenAdded { get; set; }

    /// <summary>
    ///     Configures the Aqueduct SignalR backplane for the specified hub type.
    /// </summary>
    /// <typeparam name="THub">The SignalR hub type.</typeparam>
    /// <returns>The Mississippi gateway builder for chaining.</returns>
    public MississippiGatewayBuilder AddAqueduct<THub>()
        where THub : Hub
    {
        if (HasAqueductBeenAdded)
        {
            return this;
        }

        HasAqueductBeenAdded = true;
        Services.AddAqueduct<THub>();
        return this;
    }

    /// <summary>
    ///     Configures the Aqueduct SignalR backplane for the specified hub type with custom options.
    /// </summary>
    /// <typeparam name="THub">The SignalR hub type.</typeparam>
    /// <param name="configure">The Aqueduct configuration callback.</param>
    /// <returns>The Mississippi gateway builder for chaining.</returns>
    public MississippiGatewayBuilder AddAqueduct<THub>(
        Action<AqueductOptions> configure
    )
        where THub : Hub
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (HasAqueductBeenAdded)
        {
            return this;
        }

        HasAqueductBeenAdded = true;
        Services.AddAqueduct<THub>(configure);
        return this;
    }

    /// <summary>
    ///     Configures Inlet gateway services with default options, including aggregate and projection infrastructure.
    /// </summary>
    /// <returns>The Mississippi gateway builder for chaining.</returns>
    public MississippiGatewayBuilder AddInletGateway() => AddInletGateway(_ => { });

    /// <summary>
    ///     Configures Inlet gateway services with custom options, including aggregate and projection infrastructure.
    /// </summary>
    /// <param name="configure">The Inlet gateway configuration callback.</param>
    /// <returns>The Mississippi gateway builder for chaining.</returns>
    public MississippiGatewayBuilder AddInletGateway(
        Action<InletServerOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (HasInletGatewayBeenAdded)
        {
            return this;
        }

        HasInletGatewayBeenAdded = true;
        Services.AddControllers();
        Services.AddInletServer(configure);
        Services.AddAggregateSupport();
        Services.AddUxProjections();
        return this;
    }

    /// <summary>
    ///     Registers the JSON serialization provider used by Mississippi aggregate infrastructure.
    /// </summary>
    /// <returns>The Mississippi gateway builder for chaining.</returns>
    public MississippiGatewayBuilder AddJsonSerialization()
    {
        if (HasJsonSerializationBeenAdded)
        {
            return this;
        }

        HasJsonSerializationBeenAdded = true;
        Services.AddJsonSerialization();
        return this;
    }

    /// <summary>
    ///     Registers domain-specific mapper services with the gateway builder.
    /// </summary>
    /// <param name="configure">The mapper registration callback.</param>
    /// <returns>The Mississippi gateway builder for chaining.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public MississippiGatewayBuilder RegisterDomainMappers(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }

    /// <summary>
    ///     Scans the provided assemblies for projection path metadata used by Inlet subscriptions.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The Mississippi gateway builder for chaining.</returns>
    public MississippiGatewayBuilder ScanProjectionAssemblies(
        params Assembly[] assemblies
    )
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        Services.ScanProjectionAssemblies(assemblies);
        return this;
    }
}
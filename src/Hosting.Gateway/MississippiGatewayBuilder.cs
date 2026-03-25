using System;
using System.Collections.Generic;
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

    private HashSet<string> RegisteredDomains { get; } = new(StringComparer.Ordinal);

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
            throw CreateDuplicateSubsystemException("Aqueduct", "AddAqueduct<THub>(...)");
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
            throw CreateDuplicateSubsystemException("Aqueduct", "AddAqueduct<THub>(...)");
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
            throw CreateDuplicateSubsystemException("Inlet gateway", "AddInletGateway(...)");
        }

        HasInletGatewayBeenAdded = true;
        Services.AddControllers();
        Services.AddInletServer(options =>
        {
            ApplySafeDefaultGeneratedApiAuthorization(options);
            configure(options);
        });
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
    ///     Registers generated domain-specific mapper services exactly once per gateway builder.
    /// </summary>
    /// <param name="domainName">The normalized domain name being attached.</param>
    /// <param name="registrationMethodName">The generated registration method name.</param>
    /// <param name="configure">The mapper registration callback.</param>
    /// <returns>The Mississippi gateway builder for chaining.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public MississippiGatewayBuilder RegisterDomainMappers(
        string domainName,
        string registrationMethodName,
        Action<IServiceCollection> configure
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainName);
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationMethodName);
        ArgumentNullException.ThrowIfNull(configure);
        if (!RegisteredDomains.Add(domainName))
        {
            throw new InvalidOperationException(
                $"Mississippi gateway domain composition for '{domainName}' can only be attached once per builder. Remove the duplicate {registrationMethodName}(...) call and keep each domain on a single gateway builder path.");
        }

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

    private static void ApplySafeDefaultGeneratedApiAuthorization(
        InletServerOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        options.GeneratedApiAuthorization.Mode = GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints;
        options.GeneratedApiAuthorization.AllowAnonymousOptOut = false;
    }

    private static InvalidOperationException CreateDuplicateSubsystemException(
        string subsystemName,
        string registrationMethodName
    ) =>
        new(
            $"Mississippi gateway {subsystemName} composition can only be attached once per builder. Remove the duplicate {registrationMethodName} call and keep each gateway subsystem on a single builder path.");
}
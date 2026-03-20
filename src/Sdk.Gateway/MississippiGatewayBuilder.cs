using System;
using System.ComponentModel;
using System.Reflection;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Gateway;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Runtime;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Gateway;
using Mississippi.Inlet.Generators.Abstractions;
using Mississippi.Inlet.Runtime;
using Mississippi.Inlet.Runtime.Abstractions;


namespace Mississippi.Sdk.Gateway;

/// <summary>
///     Root builder for Mississippi gateway-side composition, attached from
///     <see cref="WebApplicationBuilderExtensions.UseMississippi" />.
/// </summary>
/// <remarks>
///     <para>
///         This builder owns gateway-specific service composition: generated API registration,
///         SignalR/integration service composition, JSON serialization, Aqueduct backplane,
///         Inlet gateway services, and gateway-specific validation/diagnostics.
///     </para>
///     <para>
///         Live endpoint mapping (SignalR hubs) remains explicit in app startup — this builder
///         does not map endpoints.
///     </para>
/// </remarks>
public sealed class MississippiGatewayBuilder
{
    private readonly IServiceCollection services;

    private bool aqueductAdded;

    private bool inletGatewayAdded;

    private bool jsonSerializationAdded;

    private ProjectionAuthorizationRegistry? projectionAuthorizationRegistry;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiGatewayBuilder" /> class.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    internal MississippiGatewayBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        this.services = services;
    }

    /// <summary>
    ///     Configures the Aqueduct SignalR backplane for real-time projection updates.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    /// <typeparam name="THub">The SignalR hub type to use for projection updates.</typeparam>
    /// <param name="configure">Action to configure Aqueduct options.</param>
    public void AddAqueduct<THub>(
        Action<AqueductOptions> configure
    )
        where THub : Hub
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (aqueductAdded)
        {
            return;
        }

        aqueductAdded = true;
        services.AddAqueduct<THub>(configure);
    }

    /// <summary>
    ///     Configures Inlet gateway services for real-time projection updates,
    ///     generated API authorization, and aggregate/projection infrastructure.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    /// <param name="configure">Action to configure Inlet server options.</param>
    public void AddInletGateway(
        Action<InletServerOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (inletGatewayAdded)
        {
            return;
        }

        inletGatewayAdded = true;
        services.AddControllers();
        services.AddInletServer(configure);
        services.AddAggregateSupport();
        services.AddUxProjections();
    }

    /// <summary>
    ///     Configures Inlet gateway services with default options.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    public void AddInletGateway()
    {
        AddInletGateway(_ => { });
    }

    /// <summary>
    ///     Registers the JSON serialization provider used by aggregate and event infrastructure.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    public void AddJsonSerialization()
    {
        if (jsonSerializationAdded)
        {
            return;
        }

        jsonSerializationAdded = true;
        JsonSerializationRegistrations.AddJsonSerialization(services);
    }

    /// <summary>
    ///     Registers domain-specific mapper services with the gateway builder.
    ///     This method is intended for use by source-generated domain registration code only.
    /// </summary>
    /// <param name="configure">A callback that receives the service collection for mapper registration.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterDomainMappers(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(services);
    }

    /// <summary>
    ///     Registers explicit projection authorization metadata for SignalR subscription checks.
    /// </summary>
    /// <typeparam name="TProjection">The projection type carrying the metadata attributes.</typeparam>
    public void RegisterProjectionAuthorization<TProjection>()
        where TProjection : class
    {
        Type projectionType = typeof(TProjection);
        ProjectionPathAttribute pathAttribute = projectionType.GetCustomAttribute<ProjectionPathAttribute>() ??
                                                throw new InvalidOperationException(
                                                    $"Projection type '{projectionType.FullName}' must declare {nameof(ProjectionPathAttribute)} to be registered.");
        GenerateAuthorizationAttribute? authorizationAttribute =
            projectionType.GetCustomAttribute<GenerateAuthorizationAttribute>();
        bool hasAuthorize = authorizationAttribute is not null;
        bool hasAllowAnonymous = projectionType.GetCustomAttribute<GenerateAllowAnonymousAttribute>() is not null;
        if (!hasAuthorize && !hasAllowAnonymous)
        {
            return;
        }

        EnsureProjectionAuthorizationRegistry();
        projectionAuthorizationRegistry!.Register(
            pathAttribute.Path,
            new(
                authorizationAttribute?.Policy,
                authorizationAttribute?.Roles,
                authorizationAttribute?.AuthenticationSchemes,
                hasAuthorize,
                hasAllowAnonymous));
    }

    /// <summary>
    ///     Gets the service collection for direct registration by generated extensions.
    /// </summary>
    /// <returns>The underlying service collection.</returns>
    internal IServiceCollection GetServices() => services;

    /// <summary>
    ///     Validates gateway builder state for correctness.
    /// </summary>
    internal void Validate()
    {
        // Gateway validation will be expanded with security inference checks.
        _ = services;
    }

    private void EnsureProjectionAuthorizationRegistry()
    {
        projectionAuthorizationRegistry ??= new();
        services.RemoveAll<IProjectionAuthorizationRegistry>();
        services.AddSingleton<IProjectionAuthorizationRegistry>(projectionAuthorizationRegistry);
    }
}
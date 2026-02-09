using System;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Builders;


namespace Mississippi.Aqueduct.Builders;

/// <summary>
///     Builder for Aqueduct server registration.
/// </summary>
public sealed class AqueductServerBuilder : IAqueductServerBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AqueductServerBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public AqueductServerBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IAqueductServerBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }

    /// <inheritdoc />
    public IAqueductServerBuilder ConfigureOptions(
        Action<AqueductOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(configure);
        return this;
    }

    /// <inheritdoc />
    public IAqueductServerBuilder AddBackplane<THub>()
        where THub : Hub
    {
        Services.TryAddSingleton<IServerIdProvider, ServerIdProvider>();
        Services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
        Services.TryAddSingleton<IConnectionRegistry, ConnectionRegistry>();
        Services.TryAddSingleton<ILocalMessageSender, LocalMessageSender>();
        Services.TryAddSingleton<IHeartbeatManager, HeartbeatManager>();
        Services.TryAddSingleton<IStreamSubscriptionManager, StreamSubscriptionManager>();
        Services.TryAddSingleton<HubLifetimeManager<THub>, AqueductHubLifetimeManager<THub>>();
        return this;
    }

    /// <inheritdoc />
    public IAqueductServerBuilder AddNotifier()
    {
        Services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
        Services.TryAddSingleton<IAqueductNotifier, AqueductNotifier>();
        return this;
    }
}

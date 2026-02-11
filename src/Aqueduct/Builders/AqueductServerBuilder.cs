using System;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Builders;
using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Aqueduct.Builders;

/// <summary>
///     Builder for Aqueduct server registration.
/// </summary>
public sealed class AqueductServerBuilder : IAqueductServerBuilder
{
    private Action<Action<IServiceCollection>> ConfigureServicesAction { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AqueductServerBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public AqueductServerBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ConfigureServicesAction = action => action(services);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AqueductServerBuilder" /> class.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    public AqueductServerBuilder(
        IMississippiServerBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ConfigureServicesAction = action => builder.ConfigureServices(action);
    }

    /// <inheritdoc />
    public IAqueductServerBuilder AddBackplane<THub>()
        where THub : Hub
    {
        ConfigureServices(services =>
        {
            services.TryAddSingleton<IServerIdProvider, ServerIdProvider>();
            services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
            services.TryAddSingleton<IConnectionRegistry, ConnectionRegistry>();
            services.TryAddSingleton<ILocalMessageSender, LocalMessageSender>();
            services.TryAddSingleton<IHeartbeatManager, HeartbeatManager>();
            services.TryAddSingleton<IStreamSubscriptionManager, StreamSubscriptionManager>();
            services.TryAddSingleton<HubLifetimeManager<THub>, AqueductHubLifetimeManager<THub>>();
        });
        return this;
    }

    /// <inheritdoc />
    public IAqueductServerBuilder AddNotifier()
    {
        ConfigureServices(services =>
        {
            services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
            services.TryAddSingleton<IAqueductNotifier, AqueductNotifier>();
        });
        return this;
    }

    /// <inheritdoc />
    public IAqueductServerBuilder ConfigureAqueductOptions(
        Action<AqueductOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        ConfigureServices(services => services.Configure(configure));
        return this;
    }

    /// <inheritdoc />
    public IAqueductServerBuilder ConfigureOptions<TOptions>(
        Action<TOptions> configure
    )
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        ConfigureServices(services => services.Configure(configure));
        return this;
    }

    /// <inheritdoc />
    public IAqueductServerBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        ConfigureServicesAction(configure);
        return this;
    }
}
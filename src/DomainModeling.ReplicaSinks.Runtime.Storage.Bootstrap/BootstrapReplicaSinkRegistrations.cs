using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;

/// <summary>
///     Provides extension methods for registering the bootstrap replica sink provider.
/// </summary>
public static class BootstrapReplicaSinkRegistrations
{
    /// <summary>
    ///     Adds the bootstrap in-memory delivery-state store used by the Increment 03a proof path.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBootstrapReplicaSinkDeliveryStateStore(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IReplicaSinkDeliveryStateStore, BootstrapReplicaSinkDeliveryStateStore>();
        return services;
    }

    /// <summary>
    ///     Adds the bootstrap replica sink provider for the specified named sink.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="clientKey">The keyed client or dependency key consumed by the provider.</param>
    /// <param name="configure">The provider options configuration delegate.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBootstrapReplicaSink(
        this IServiceCollection services,
        string sinkKey,
        string clientKey,
        Action<BootstrapReplicaSinkOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentNullException.ThrowIfNull(clientKey);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientKey);
        services.Configure<BootstrapReplicaSinkOptions>(sinkKey, configure);
        services.PostConfigure<BootstrapReplicaSinkOptions>(sinkKey, options => options.ClientKey = clientKey);
        return services.AddBootstrapReplicaSinkCore(sinkKey);
    }

    /// <summary>
    ///     Adds the bootstrap replica sink provider for the specified named sink using configuration binding.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="clientKey">The keyed client or dependency key consumed by the provider.</param>
    /// <param name="configurationSection">The configuration section that supplies provider options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBootstrapReplicaSink(
        this IServiceCollection services,
        string sinkKey,
        string clientKey,
        IConfiguration configurationSection
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentNullException.ThrowIfNull(clientKey);
        ArgumentNullException.ThrowIfNull(configurationSection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientKey);
        services.Configure<BootstrapReplicaSinkOptions>(sinkKey, configurationSection);
        services.PostConfigure<BootstrapReplicaSinkOptions>(sinkKey, options => options.ClientKey = clientKey);
        return services.AddBootstrapReplicaSinkCore(sinkKey);
    }

    private static IServiceCollection AddBootstrapReplicaSinkCore(
        this IServiceCollection services,
        string sinkKey
    )
    {
        services.AddLogging();
        services.AddBootstrapReplicaSinkDeliveryStateStore();
        services.AddKeyedSingleton<IReplicaSinkProvider>(
            sinkKey,
            (
                provider,
                _
            ) =>
            {
                BootstrapReplicaSinkOptions options =
                    provider.GetRequiredService<IOptionsMonitor<BootstrapReplicaSinkOptions>>().Get(sinkKey);
                ILogger<BootstrapReplicaSinkProvider> logger =
                    provider.GetRequiredService<ILogger<BootstrapReplicaSinkProvider>>();
                return new BootstrapReplicaSinkProvider(sinkKey, options, logger);
            });
        services.AddSingleton(provider =>
        {
            BootstrapReplicaSinkOptions options =
                provider.GetRequiredService<IOptionsMonitor<BootstrapReplicaSinkOptions>>().Get(sinkKey);
            return new ReplicaSinkRegistrationDescriptor(
                sinkKey,
                options.ClientKey,
                BootstrapReplicaSinkProvider.FormatName,
                typeof(BootstrapReplicaSinkProvider),
                options.ProvisioningMode);
        });
        return services;
    }
}
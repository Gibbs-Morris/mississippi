using System;
using System.Linq;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Common.Runtime.Storage.Abstractions.Retry;
using Mississippi.Common.Runtime.Storage.Cosmos.Retry;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Provides extension methods for registering the Cosmos-backed replica sink provider.
/// </summary>
public static class CosmosReplicaSinkRegistrations
{
    /// <summary>
    ///     Adds a Cosmos-backed replica sink for the specified named sink registration.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="clientKey">The keyed Cosmos client registration key consumed by the provider.</param>
    /// <param name="configure">The options configuration delegate.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    ///     Callers must register a keyed <see cref="CosmosClient" /> using the supplied <paramref name="clientKey" />.
    /// </remarks>
    public static IServiceCollection AddCosmosReplicaSink(
        this IServiceCollection services,
        string sinkKey,
        string clientKey,
        Action<CosmosReplicaSinkOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ValidateKeys(sinkKey, clientKey);
        services.AddOptions<CosmosReplicaSinkOptions>(sinkKey).Configure(configure).ValidateOnStart();
        services.PostConfigure<CosmosReplicaSinkOptions>(sinkKey, options => options.ClientKey = clientKey);
        return services.AddCosmosReplicaSinkCore(sinkKey);
    }

    /// <summary>
    ///     Adds a Cosmos-backed replica sink for the specified named sink registration using configuration binding.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="clientKey">The keyed Cosmos client registration key consumed by the provider.</param>
    /// <param name="configurationSection">The configuration section supplying provider options.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    ///     Callers must register a keyed <see cref="CosmosClient" /> using the supplied <paramref name="clientKey" />.
    /// </remarks>
    public static IServiceCollection AddCosmosReplicaSink(
        this IServiceCollection services,
        string sinkKey,
        string clientKey,
        IConfiguration configurationSection
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);
        ValidateKeys(sinkKey, clientKey);
        services.AddOptions<CosmosReplicaSinkOptions>(sinkKey).Bind(configurationSection).ValidateOnStart();
        services.PostConfigure<CosmosReplicaSinkOptions>(sinkKey, options => options.ClientKey = clientKey);
        return services.AddCosmosReplicaSinkCore(sinkKey);
    }

    private static IServiceCollection AddCosmosReplicaSinkCore(
        this IServiceCollection services,
        string sinkKey
    )
    {
        services.AddLogging();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IRetryPolicy, CosmosRetryPolicy>();
        services.TryAddEnumerable(
            ServiceDescriptor
                .Singleton<IValidateOptions<CosmosReplicaSinkOptions>, CosmosReplicaSinkOptionsValidation>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CosmosReplicaSinkContainerInitializer>());
        EnsureDeliveryStateStoreOwnership(services);
        services.TryAddSingleton<IReplicaSinkDeliveryStateStore, CosmosReplicaSinkDeliveryStateStore>();
        string containerServiceKey = ReplicaSinkCosmosDefaults.CreateContainerServiceKey(sinkKey);
        services.AddKeyedSingleton<Container>(
            containerServiceKey,
            (
                provider,
                _
            ) =>
            {
                CosmosReplicaSinkOptions options =
                    provider.GetRequiredService<IOptionsMonitor<CosmosReplicaSinkOptions>>().Get(sinkKey);
                CosmosClient cosmosClient = provider.GetRequiredKeyedService<CosmosClient>(options.ClientKey);
                return cosmosClient.GetDatabase(options.DatabaseId).GetContainer(options.ContainerId);
            });
        services.AddSingleton<ICosmosReplicaSinkShard>(provider =>
        {
            CosmosReplicaSinkOptions options =
                provider.GetRequiredService<IOptionsMonitor<CosmosReplicaSinkOptions>>().Get(sinkKey);
            CosmosClient cosmosClient = provider.GetRequiredKeyedService<CosmosClient>(options.ClientKey);
            Container container = provider.GetRequiredKeyedService<Container>(containerServiceKey);
            IRetryPolicy retryPolicy = provider.GetRequiredService<IRetryPolicy>();
            TimeProvider timeProvider = provider.GetRequiredService<TimeProvider>();
            ILogger<CosmosReplicaSinkContainerOperations> operationsLogger =
                provider.GetRequiredService<ILogger<CosmosReplicaSinkContainerOperations>>();
            ILogger<CosmosReplicaSinkProvider> providerLogger =
                provider.GetRequiredService<ILogger<CosmosReplicaSinkProvider>>();
            CosmosReplicaSinkContainerOperations operations = new(
                sinkKey,
                options,
                cosmosClient,
                container,
                retryPolicy,
                timeProvider,
                operationsLogger);
            return new CosmosReplicaSinkProvider(sinkKey, options, operations, providerLogger);
        });
        services.AddKeyedSingleton<IReplicaSinkProvider>(
            sinkKey,
            (
                provider,
                _
            ) => (IReplicaSinkProvider)provider.GetServices<ICosmosReplicaSinkShard>()
                .Single(shard => string.Equals(shard.SinkKey, sinkKey, StringComparison.Ordinal)));
        services.AddSingleton(provider =>
        {
            CosmosReplicaSinkOptions options =
                provider.GetRequiredService<IOptionsMonitor<CosmosReplicaSinkOptions>>().Get(sinkKey);
            return new ReplicaSinkRegistrationDescriptor(
                sinkKey,
                options.ClientKey,
                ReplicaSinkCosmosDefaults.FormatName,
                typeof(CosmosReplicaSinkProvider),
                options.ProvisioningMode);
        });
        return services;
    }

    private static void EnsureDeliveryStateStoreOwnership(
        IServiceCollection services
    )
    {
        ServiceDescriptor? existingDescriptor = services.LastOrDefault(descriptor =>
            descriptor.ServiceType == typeof(IReplicaSinkDeliveryStateStore));
        if (existingDescriptor is null)
        {
            return;
        }

        Type? existingImplementationType = existingDescriptor.ImplementationType ??
                                           existingDescriptor.ImplementationInstance?.GetType();
        if (existingImplementationType == typeof(CosmosReplicaSinkDeliveryStateStore))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Replica sink delivery-state store '{existingImplementationType?.FullName ?? "<factory>"}' is already registered for '{nameof(IReplicaSinkDeliveryStateStore)}'. Cosmos registrations require '{typeof(CosmosReplicaSinkDeliveryStateStore).FullName}' and cannot replace an existing implementation.");
    }

    private static void ValidateKeys(
        string sinkKey,
        string clientKey
    )
    {
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentNullException.ThrowIfNull(clientKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientKey);
    }
}
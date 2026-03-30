using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Hosted service that validates or provisions Cosmos replica sink containers during host startup.
/// </summary>
internal sealed class CosmosReplicaSinkContainerInitializer : IHostedService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkContainerInitializer" /> class.
    /// </summary>
    /// <param name="shards">The registered per-sink Cosmos shards.</param>
    /// <param name="logger">The initializer logger.</param>
    public CosmosReplicaSinkContainerInitializer(
        IEnumerable<ICosmosReplicaSinkShard> shards,
        ILogger<CosmosReplicaSinkContainerInitializer> logger
    )
    {
        ArgumentNullException.ThrowIfNull(shards);
        ArgumentNullException.ThrowIfNull(logger);
        Shards = shards.ToArray();
        Logger = logger;
    }

    private ILogger<CosmosReplicaSinkContainerInitializer> Logger { get; }

    private ICosmosReplicaSinkShard[] Shards { get; }

    /// <inheritdoc />
    public async Task StartAsync(
        CancellationToken cancellationToken
    )
    {
        Logger.LogInitializingContainers(Shards.Length);
        foreach (ICosmosReplicaSinkShard shard in Shards)
        {
            await shard.EnsureContainerAsync(cancellationToken).ConfigureAwait(false);
        }

        Logger.LogInitializedContainers(Shards.Length);
    }

    /// <inheritdoc />
    public Task StopAsync(
        CancellationToken cancellationToken
    ) =>
        Task.CompletedTask;
}
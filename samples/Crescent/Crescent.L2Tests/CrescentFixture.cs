using Microsoft.Extensions.Hosting;

using Mississippi.Common.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos;


namespace Crescent.Crescent.L2Tests;

/// <summary>
///     xUnit fixture that starts the Crescent AppHost with Cosmos DB Snapshot Storage.
/// </summary>
#pragma warning disable CA1515 // Types can be made internal - xUnit fixture must be public
public sealed class CrescentFixture : CrescentFixtureBase
{
    /// <inheritdoc />
    protected override void AddSnapshotStorage(IHostApplicationBuilder builder)
    {
        builder.Services.AddCosmosSnapshotStorageProvider(options =>
        {
            options.CosmosClientServiceKey = MississippiDefaults.ServiceKeys.CosmosSnapshotsClient;
            options.DatabaseId = "aspire-l2tests";
            options.ContainerId = MississippiDefaults.ContainerIds.Snapshots;
            options.QueryBatchSize = 100;
        });
    }
}
#pragma warning restore CA1515

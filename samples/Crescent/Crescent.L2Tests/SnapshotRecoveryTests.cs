using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Verifies Cosmos snapshot recovery through fresh cache-grain activations.
/// </summary>
[Collection(CrescentTestCollection.Name)]
#pragma warning disable CA1515 // xUnit test class must be public.
public sealed class SnapshotRecoveryTests
#pragma warning restore CA1515
{
    private readonly CrescentFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotRecoveryTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    public SnapshotRecoveryTests(
        CrescentFixture fixture
    ) =>
        this.fixture = fixture;

    private static bool IsCounterAggregateMeasurement(
        SnapshotMetricMeasurement measurement
    ) =>
        measurement.Tags.TryGetValue("snapshot.type", out object? snapshotType) &&
        (snapshotType as string == nameof(CounterAggregate));

    private async Task<SnapshotEnvelope> WaitForSnapshotAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken
    )
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        using CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        try
        {
            while (true)
            {
                SnapshotEnvelope? snapshot = await fixture.SnapshotStorageReader.ReadAsync(
                    snapshotKey,
                    linkedCancellation.Token);
                if (snapshot is not null)
                {
                    return snapshot;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCancellation.Token);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested &&
                                                 !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Snapshot version {snapshotKey.Version} was not persisted within 30 seconds.");
        }
    }

    /// <summary>
    ///     Verifies a persisted checkpoint is reused after a silo restart and only the event tail is replayed.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task PersistedCheckpointIsReusedAfterRestartAndOnlyTailEventsReplay()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string entityId = $"snapshot-recovery-{Guid.NewGuid():N}";
        IGenericAggregateGrain<CounterAggregate> aggregate =
            fixture.AggregateGrainFactory.GetGenericAggregate<CounterAggregate>(entityId);
        OperationResult initializeResult = await aggregate.ExecuteAsync(new InitializeCounter(), cancellationToken);
        Assert.True(initializeResult.Success);
        for (int position = 1; position <= 149; position++)
        {
            OperationResult incrementResult = await aggregate.ExecuteAsync(new IncrementCounter(), cancellationToken);
            Assert.True(incrementResult.Success, $"Increment at position {position} should succeed.");
        }

        string reducerHash = fixture.CounterRootReducer.GetReducerHash();
        SnapshotStreamKey streamKey = new(
            BrookNameHelper.GetBrookName<CounterAggregate>(),
            SnapshotStorageNameHelper.GetStorageName<CounterAggregate>(),
            entityId,
            reducerHash);
        SnapshotKey checkpointKey = new(streamKey, 100);

        // Activating version 100 builds it from the production brook/reducers when necessary and enqueues persistence.
        ISnapshotCacheGrain<CounterAggregate> initialCheckpointGrain =
            fixture.ClusterClient.GetGrain<ISnapshotCacheGrain<CounterAggregate>>(checkpointKey.ToString());
        CounterAggregate initialCheckpoint = await initialCheckpointGrain.GetStateAsync(cancellationToken);
        Assert.Equal(100, initialCheckpoint.Count);
        Assert.Equal(100, initialCheckpoint.IncrementCount);

        // Poll the actual Cosmos-backed provider; the one-way persister call alone is not evidence of durability.
        SnapshotEnvelope storedCheckpoint = await WaitForSnapshotAsync(checkpointKey, cancellationToken);
        Assert.Equal(reducerHash, storedCheckpoint.ReducerHash);
        CounterAggregate storedState = fixture.CounterSnapshotStateConverter.FromEnvelope(storedCheckpoint);
        Assert.Equal(100, storedState.Count);
        Assert.Equal(100, storedState.IncrementCount);

        // Recreate the silo while keeping the Aspire-owned Cosmos and Azure Storage resources intact.
        await fixture.RestartOrleansHostAsync(cancellationToken);
        Assert.Equal(reducerHash, fixture.CounterRootReducer.GetReducerHash());
        Assert.Equal(
            reducerHash,
            (await fixture.SnapshotStorageReader.ReadAsync(checkpointKey, cancellationToken))?.ReducerHash);
        SnapshotKey missingTailKey = new(streamKey, 149);
        await fixture.SnapshotStorageWriter.DeleteAsync(missingTailKey, cancellationToken);
        Assert.Null(await fixture.SnapshotStorageReader.ReadAsync(missingTailKey, cancellationToken));
        using SnapshotMetricCapture metrics = new();
        int beforeCheckpointRead = metrics.Snapshot().Count;
        ISnapshotCacheGrain<CounterAggregate> coldCheckpointGrain =
            fixture.ClusterClient.GetGrain<ISnapshotCacheGrain<CounterAggregate>>(checkpointKey.ToString());
        CounterAggregate recoveredCheckpoint = await coldCheckpointGrain.GetStateAsync(cancellationToken);
        SnapshotMetricMeasurement[] checkpointReadMeasurements = metrics.Snapshot()
            .Skip(beforeCheckpointRead)
            .ToArray();
        SnapshotMetricMeasurement[] checkpointMeasurements = checkpointReadMeasurements
            .Where(IsCounterAggregateMeasurement)
            .ToArray();
        Assert.Equal(100, recoveredCheckpoint.Count);
        Assert.Equal(100, recoveredCheckpoint.IncrementCount);
        Assert.Contains(
            checkpointMeasurements,
            measurement => (measurement.InstrumentName == "snapshot.cache.hits") && (measurement.Value == 1));
        Assert.DoesNotContain(
            checkpointMeasurements,
            measurement => measurement.InstrumentName == "snapshot.rebuild.events");
        Assert.DoesNotContain(
            checkpointReadMeasurements,
            measurement => measurement.InstrumentName == "snapshot.persist.count");
        int beforeTailRead = metrics.Snapshot().Count;
        ISnapshotCacheGrain<CounterAggregate> tailGrain = fixture.ClusterClient
            .GetGrain<ISnapshotCacheGrain<CounterAggregate>>(missingTailKey.ToString());
        CounterAggregate recoveredTail = await tailGrain.GetStateAsync(cancellationToken);
        SnapshotMetricMeasurement[] tailMeasurements = metrics.Snapshot()
            .Skip(beforeTailRead)
            .Where(IsCounterAggregateMeasurement)
            .ToArray();
        Assert.Equal(149, recoveredTail.Count);
        Assert.Equal(149, recoveredTail.IncrementCount);
        Assert.Contains(tailMeasurements, measurement => measurement.InstrumentName == "snapshot.base.used");
        Assert.Contains(
            tailMeasurements,
            measurement => (measurement.InstrumentName == "snapshot.cache.misses") && (measurement.Value == 1));
        SnapshotMetricMeasurement replayMeasurement = Assert.Single(
            tailMeasurements,
            measurement => measurement.InstrumentName == "snapshot.rebuild.events");
        Assert.Equal(49, replayMeasurement.Value);
    }
}
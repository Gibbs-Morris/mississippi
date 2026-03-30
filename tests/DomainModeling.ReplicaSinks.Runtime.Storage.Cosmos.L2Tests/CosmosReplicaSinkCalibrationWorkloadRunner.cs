namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Executes deterministic replica-sink provider workloads against the real Cosmos emulator path.
/// </summary>
internal sealed class CosmosReplicaSinkCalibrationWorkloadRunner
{
    private readonly string cosmosConnectionString;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkCalibrationWorkloadRunner" /> class.
    /// </summary>
    /// <param name="cosmosConnectionString">The emulator-backed Cosmos connection string.</param>
    public CosmosReplicaSinkCalibrationWorkloadRunner(
        string cosmosConnectionString
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cosmosConnectionString);
        this.cosmosConnectionString = cosmosConnectionString;
    }

    /// <summary>
    ///     Runs the specified deterministic calibration scenario against the real provider path.
    /// </summary>
    /// <param name="scenario">The scenario to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The scenario-local metrics captured during execution.</returns>
    public async Task<CosmosReplicaSinkCalibrationScenarioMetrics> RunAsync(
        CosmosReplicaSinkCalibrationScenario scenario,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(scenario);

        using CosmosClient cosmosClient = CreateCosmosClient();
        await using CosmosReplicaSinkCalibrationScenarioHarness harness =
            await CosmosReplicaSinkCalibrationScenarioHarness.CreateAsync(cosmosClient, scenario, cancellationToken);
        await EnsureTargetsAsync(harness, cancellationToken);

        List<CosmosReplicaSinkCalibrationWriteWorkItem> backlogOperations =
            BuildReplayOperations(harness.Registrations, scenario);
        Stopwatch replayStopwatch = Stopwatch.StartNew();
        await ExecuteWritesAsync(harness, backlogOperations, scenario.InFlightBudget, cancellationToken);
        replayStopwatch.Stop();

        List<CosmosReplicaSinkCalibrationWriteWorkItem> liveOperations =
            BuildLiveOperations(harness.Registrations, scenario);
        double? liveWriteElapsedMilliseconds = null;
        if (liveOperations.Count > 0)
        {
            Stopwatch liveStopwatch = Stopwatch.StartNew();
            await ExecuteWritesAsync(harness, liveOperations, scenario.InFlightBudget, cancellationToken);
            liveStopwatch.Stop();
            liveWriteElapsedMilliseconds = liveStopwatch.Elapsed.TotalMilliseconds;
        }

        IReadOnlyDictionary<string, CosmosReplicaSinkCalibrationWriteWorkItem> finalWritesBySink =
            backlogOperations.Concat(liveOperations)
                .GroupBy(static item => item.SinkKey, StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.OrderBy(static item => item.SourcePosition).Last(),
                    StringComparer.Ordinal);

        List<CosmosReplicaSinkCalibrationTargetMetrics> targetMetrics =
            await ReadTargetMetricsAsync(harness, finalWritesBySink, cancellationToken);
        int retryCount = (await harness.StateStore.ReadDueRetriesAsync(
                DateTimeOffset.MaxValue,
                scenario.TotalWriteCount,
                cancellationToken))
            .Count;
        ReplicaSinkDeliveryStatePage deadLetters = await harness.StateStore.ReadDeadLetterPageAsync(
            scenario.TotalWriteCount,
            cancellationToken: cancellationToken);

        return new(
            scenario.Name,
            scenario.EntityCount,
            scenario.SinkCount,
            scenario.PayloadSizeClass.ToString(),
            scenario.TotalWriteCount,
            scenario.InFlightBudget,
            replayStopwatch.Elapsed.TotalMilliseconds,
            liveWriteElapsedMilliseconds,
            retryCount,
            deadLetters.Items.Count,
            targetMetrics);
    }

    private static List<CosmosReplicaSinkCalibrationWriteWorkItem> BuildLiveOperations(
        IReadOnlyList<CosmosReplicaSinkCalibrationSinkRegistration> registrations,
        CosmosReplicaSinkCalibrationScenario scenario
    )
    {
        if (scenario.LiveWriteCount == 0)
        {
            return [];
        }

        List<CosmosReplicaSinkCalibrationWriteWorkItem> operations = [];
        for (int liveIndex = 0; liveIndex < scenario.LiveWriteCount; liveIndex++)
        {
            int entityIndex = liveIndex % scenario.EntityCount;
            long sourcePosition = scenario.EntityCount + liveIndex + 1L;
            foreach (CosmosReplicaSinkCalibrationSinkRegistration registration in registrations)
            {
                operations.Add(
                    new(
                        registration.SinkKey,
                        registration.Target,
                        CosmosReplicaSinkCalibrationDefaults.CreateDeliveryKey(registration.SinkKey, entityIndex),
                        sourcePosition,
                        CreatePayload(scenario, entityIndex, sourcePosition, isLivePhase: true)));
            }
        }

        return operations;
    }

    private static List<CosmosReplicaSinkCalibrationWriteWorkItem> BuildReplayOperations(
        IReadOnlyList<CosmosReplicaSinkCalibrationSinkRegistration> registrations,
        CosmosReplicaSinkCalibrationScenario scenario
    )
    {
        List<CosmosReplicaSinkCalibrationWriteWorkItem> operations = [];
        for (int entityIndex = 0; entityIndex < scenario.EntityCount; entityIndex++)
        {
            long sourcePosition = entityIndex + 1L;
            foreach (CosmosReplicaSinkCalibrationSinkRegistration registration in registrations)
            {
                operations.Add(
                    new(
                        registration.SinkKey,
                        registration.Target,
                        CosmosReplicaSinkCalibrationDefaults.CreateDeliveryKey(registration.SinkKey, entityIndex),
                        sourcePosition,
                        CreatePayload(scenario, entityIndex, sourcePosition, isLivePhase: false)));
            }
        }

        return operations;
    }

    private CosmosClient CreateCosmosClient() =>
        new(
            cosmosConnectionString,
            new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true,
            });

    private static CosmosReplicaSinkCalibrationPayload CreatePayload(
        CosmosReplicaSinkCalibrationScenario scenario,
        int entityIndex,
        long sourcePosition,
        bool isLivePhase
    )
    {
        string entityId = $"entity-{entityIndex:D4}";
        string prefix = $"{scenario.Name}|{entityId}|{sourcePosition}|{(isLivePhase ? "live" : "replay")}|";
        int bodyLength = Math.Max(8, scenario.PayloadBytes - prefix.Length);
        char bodyCharacter = (char)('A' + ((scenario.Seed + entityIndex) % 26));
        string body = new(bodyCharacter, bodyLength);
        return new(
            entityId,
            sourcePosition,
            scenario.PayloadBytes,
            isLivePhase,
            prefix + body);
    }

    private static async Task EnsureTargetsAsync(
        CosmosReplicaSinkCalibrationScenarioHarness harness,
        CancellationToken cancellationToken
    )
    {
        foreach (CosmosReplicaSinkCalibrationSinkRegistration registration in harness.Registrations)
        {
            IReplicaSinkProvider provider = harness.Providers[registration.SinkKey];
            await provider.EnsureTargetAsync(registration.Target, cancellationToken);
        }
    }

    private static async Task ExecuteWritesAsync(
        CosmosReplicaSinkCalibrationScenarioHarness harness,
        IReadOnlyList<CosmosReplicaSinkCalibrationWriteWorkItem> operations,
        int inFlightBudget,
        CancellationToken cancellationToken
    )
    {
        using SemaphoreSlim gate = new(inFlightBudget, inFlightBudget);
        Task[] writes = operations.Select(
                async operation =>
                {
                    await gate.WaitAsync(cancellationToken);
                    try
                    {
                        await ExecuteWriteAsync(harness, operation, cancellationToken);
                    }
                    finally
                    {
                        _ = gate.Release();
                    }
                })
            .ToArray();
        await Task.WhenAll(writes);
    }

    private static async Task ExecuteWriteAsync(
        CosmosReplicaSinkCalibrationScenarioHarness harness,
        CosmosReplicaSinkCalibrationWriteWorkItem operation,
        CancellationToken cancellationToken
    )
    {
        IReplicaSinkProvider provider = harness.Providers[operation.SinkKey];
        ReplicaWriteResult result = await provider.WriteAsync(
            new(
                operation.Target,
                operation.DeliveryKey,
                operation.SourcePosition,
                ReplicaWriteMode.LatestState,
                CosmosReplicaSinkCalibrationDefaults.ContractIdentity,
                operation.Payload),
            cancellationToken);
        if (result.Outcome != ReplicaWriteOutcome.Applied)
        {
            throw new InvalidOperationException(
                $"Scenario write '{operation.DeliveryKey}' completed with unexpected outcome '{result.Outcome}'.");
        }

        await harness.StateStore.WriteAsync(
            new(
                operation.DeliveryKey,
                desiredSourcePosition: operation.SourcePosition,
                committedSourcePosition: operation.SourcePosition),
            cancellationToken);
    }

    private static async Task<List<CosmosReplicaSinkCalibrationTargetMetrics>> ReadTargetMetricsAsync(
        CosmosReplicaSinkCalibrationScenarioHarness harness,
        IReadOnlyDictionary<string, CosmosReplicaSinkCalibrationWriteWorkItem> finalWritesBySink,
        CancellationToken cancellationToken
    )
    {
        List<CosmosReplicaSinkCalibrationTargetMetrics> metrics = [];
        foreach (CosmosReplicaSinkCalibrationSinkRegistration registration in harness.Registrations)
        {
            IReplicaSinkProvider provider = harness.Providers[registration.SinkKey];
            ReplicaTargetInspection inspection = await provider.InspectAsync(registration.Target, cancellationToken);
            ReplicaSinkDeliveryState? state = await harness.StateStore.ReadAsync(
                finalWritesBySink[registration.SinkKey].DeliveryKey,
                cancellationToken);
            metrics.Add(
                new(
                    registration.SinkKey,
                    registration.ContainerId,
                    registration.Target.DestinationIdentity.TargetName,
                    inspection.TargetExists,
                    inspection.WriteCount,
                    inspection.LatestSourcePosition,
                    state?.CommittedSourcePosition));
        }

        return metrics;
    }
}

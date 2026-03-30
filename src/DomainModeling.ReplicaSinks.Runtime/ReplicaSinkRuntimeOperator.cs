using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

internal sealed class ReplicaSinkRuntimeOperator : IReplicaSinkRuntimeOperator
{
    public ReplicaSinkRuntimeOperator(
        IReplicaSinkDeliveryStateStore deliveryStateStore,
        IReplicaSinkRuntimeCoordinator coordinator,
        IReplicaSinkProjectionRegistry registry,
        IReplicaSinkOperatorAuditSink auditSink,
        IReplicaSinkExecutionHealthManager healthManager,
        IOptions<ReplicaSinkRuntimeOptions> runtimeOptions,
        ILogger<ReplicaSinkRuntimeOperator> logger
    )
    {
        DeliveryStateStore = deliveryStateStore ?? throw new ArgumentNullException(nameof(deliveryStateStore));
        Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        AuditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
        HealthManager = healthManager ?? throw new ArgumentNullException(nameof(healthManager));
        RuntimeOptions = runtimeOptions ?? throw new ArgumentNullException(nameof(runtimeOptions));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IReplicaSinkOperatorAuditSink AuditSink { get; }

    private IReplicaSinkRuntimeCoordinator Coordinator { get; }

    private IReplicaSinkDeliveryStateStore DeliveryStateStore { get; }

    private IReplicaSinkExecutionHealthManager HealthManager { get; }

    private ILogger<ReplicaSinkRuntimeOperator> Logger { get; }

    private IReplicaSinkProjectionRegistry Registry { get; }

    private IOptions<ReplicaSinkRuntimeOptions> RuntimeOptions { get; }

    public async Task<ReplicaSinkDeadLetterPage> ReadDeadLettersAsync(
        ReplicaSinkDeadLetterQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        int effectivePageSize = Math.Min(
            query.PageSize,
            Math.Max(1, RuntimeOptions.Value.MaxDeadLetterPageSize));
        bool canSeeFailureSummary =
            query.Context.AccessLevel >= ReplicaSinkOperatorAccessLevel.Detail && query.IncludeFailureSummary;
        ReplicaSinkDeliveryStatePage statePage = await DeliveryStateStore.ReadDeadLetterPageAsync(
            effectivePageSize,
            query.ContinuationToken,
            cancellationToken);
        ReplicaSinkDeadLetterRecord[] items = statePage.Items
            .Select(state => CreateRecord(state, canSeeFailureSummary))
            .ToArray();
        bool redactedFailureSummary = query.IncludeFailureSummary && !canSeeFailureSummary;
        await AuditSink.RecordDeadLetterReadAsync(
            query,
            effectivePageSize,
            items.Length,
            canSeeFailureSummary,
            redactedFailureSummary,
            cancellationToken);
        return new(items, statePage.ContinuationToken);
    }

    public async Task<ReplicaSinkDeadLetterReDriveResult> ReDriveAsync(
        ReplicaSinkDeadLetterReDriveRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Context.AccessLevel < ReplicaSinkOperatorAccessLevel.Admin)
        {
            throw new UnauthorizedAccessException("Replica sink dead-letter re-drive requires admin access.");
        }

        ReplicaSinkDeliveryState? state = await DeliveryStateStore.ReadAsync(request.DeliveryKey, cancellationToken);
        if (state is null)
        {
            ReplicaSinkDeadLetterReDriveResult notFound = new(request.DeliveryKey, "delivery_key_not_found", false);
            await AuditSink.RecordReDriveAsync(request, notFound, cancellationToken);
            return notFound;
        }

        if (state.DeadLetter is null)
        {
            ReplicaSinkDeadLetterReDriveResult nothingToReDrive = new(request.DeliveryKey, "no_dead_letter", false);
            await AuditSink.RecordReDriveAsync(request, nothingToReDrive, cancellationToken);
            return nothingToReDrive;
        }

        long targetSourcePosition = state.DesiredSourcePosition ?? state.DeadLetter.SourcePosition;
        ReplicaSinkDeliveryKeyParser.ParsedReplicaSinkDeliveryKey parsedDeliveryKey = ReplicaSinkDeliveryKeyParser.Parse(
            request.DeliveryKey);
        if (!TryResolveProjectionType(parsedDeliveryKey.ProjectionTypeName, out Type? projectionType))
        {
            ReplicaSinkDeadLetterReDriveResult projectionMissing = new(
                request.DeliveryKey,
                "projection_type_not_found",
                false,
                targetSourcePosition);
            await AuditSink.RecordReDriveAsync(request, projectionMissing, cancellationToken);
            return projectionMissing;
        }

        await Coordinator.NotifyLiveAsync(projectionType!, parsedDeliveryKey.EntityId, targetSourcePosition, cancellationToken);

        try
        {
            ReplicaSinkDeliveryState latestState = await DeliveryStateStore.ReadAsync(request.DeliveryKey, cancellationToken) ??
                                                   throw new InvalidOperationException(
                                                       $"Replica sink delivery state '{request.DeliveryKey}' was missing after re-drive notify.");
            ReplicaSinkDeliveryState updatedState = ReplicaSinkDeliveryStateTransitions.ClearDeadLetter(latestState);
            await DeliveryStateStore.WriteAsync(updatedState, cancellationToken);
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            HealthManager.Quarantine(
                parsedDeliveryKey.SinkKey,
                "dead_letter_store_failed",
                "Dead-letter persistence failed while clearing dead-letter state.");
            Logger.DeadLetterClearStoreQuarantined(parsedDeliveryKey.SinkKey, request.DeliveryKey, ex);
            ReplicaSinkDeadLetterReDriveResult quarantined = new(
                request.DeliveryKey,
                "dead_letter_store_quarantined",
                false,
                targetSourcePosition);
            await AuditSink.RecordReDriveAsync(request, quarantined, cancellationToken);
            return quarantined;
        }

        ReplicaSinkDeadLetterReDriveResult queued = new(request.DeliveryKey, "queued", true, targetSourcePosition);
        await AuditSink.RecordReDriveAsync(request, queued, cancellationToken);
        return queued;
    }

    private static ReplicaSinkDeadLetterRecord CreateRecord(
        ReplicaSinkDeliveryState state,
        bool includeFailureSummary
    )
    {
        ReplicaSinkStoredFailure deadLetter = state.DeadLetter ??
                                              throw new InvalidOperationException(
                                                  "Dead-letter page contained a state without dead-letter payload.");
        ReplicaSinkDeliveryKeyParser.ParsedReplicaSinkDeliveryKey parsedDeliveryKey = ReplicaSinkDeliveryKeyParser.Parse(
            state.DeliveryKey);
        return new(
            state.DeliveryKey,
            parsedDeliveryKey.ProjectionTypeName,
            parsedDeliveryKey.SinkKey,
            parsedDeliveryKey.TargetName,
            parsedDeliveryKey.EntityId,
            deadLetter.SourcePosition,
            deadLetter.AttemptCount,
            deadLetter.FailureCode,
            includeFailureSummary ? deadLetter.FailureSummary : null,
            !includeFailureSummary,
            deadLetter.RecordedAtUtc,
            state.DesiredSourcePosition,
            state.CommittedSourcePosition);
    }

    private static bool IsCriticalException(
        Exception exception
    ) =>
        exception is OutOfMemoryException or StackOverflowException or ThreadInterruptedException;

    private bool TryResolveProjectionType(
        string projectionTypeName,
        out Type? projectionType
    )
    {
        projectionType = Registry.GetBindingDescriptors()
            .Where(binding => string.Equals(binding.Identity.ProjectionTypeName, projectionTypeName, StringComparison.Ordinal))
            .Select(binding => binding.ProjectionType)
            .FirstOrDefault();
        return projectionType is not null;
    }
}

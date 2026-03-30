using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;

/// <summary>
///     Provides an in-memory replica sink provider for runnable onboarding proofs.
/// </summary>
public sealed class BootstrapReplicaSinkProvider : IReplicaSinkProvider
{
    /// <summary>
    ///     The informational format name exposed by the bootstrap provider.
    /// </summary>
    public const string FormatName = "bootstrap";

    /// <summary>
    ///     Initializes a new instance of the <see cref="BootstrapReplicaSinkProvider" /> class.
    /// </summary>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="options">The configured provider options.</param>
    /// <param name="logger">The provider logger.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="sinkKey" />, <paramref name="options" />, or
    ///     <paramref name="logger" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sinkKey" /> is empty or whitespace.</exception>
    public BootstrapReplicaSinkProvider(
        string sinkKey,
        BootstrapReplicaSinkOptions options,
        ILogger<BootstrapReplicaSinkProvider> logger
    )
    {
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        SinkKey = sinkKey;
        Options = options;
        Logger = logger;
    }

    /// <inheritdoc />
    public string Format => FormatName;

    private ILogger<BootstrapReplicaSinkProvider> Logger { get; }

    private BootstrapReplicaSinkOptions Options { get; }

    private string SinkKey { get; }

    private ConcurrentDictionary<ReplicaDestinationIdentity, BootstrapReplicaTargetState> Targets { get; } = new();

    /// <inheritdoc />
    public ValueTask EnsureTargetAsync(
        ReplicaTargetDescriptor target,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateClientKey(target.DestinationIdentity);
        Logger.LogEnsuringTarget(
            SinkKey,
            target.DestinationIdentity.ClientKey,
            target.DestinationIdentity.TargetName,
            target.ProvisioningMode.ToString());
        BootstrapReplicaTargetState state = Targets.GetOrAdd(target.DestinationIdentity, _ => new());
        lock (state.SyncRoot)
        {
            if (state.TargetExists)
            {
                Logger.LogTargetValidated(
                    SinkKey,
                    target.DestinationIdentity.ClientKey,
                    target.DestinationIdentity.TargetName);
                return ValueTask.CompletedTask;
            }

            if (target.ProvisioningMode != ReplicaProvisioningMode.CreateIfMissing)
            {
                throw new InvalidOperationException(
                    $"Bootstrap replica sink '{SinkKey}' target '{target.DestinationIdentity}' does not exist and provisioning mode '{target.ProvisioningMode}' does not allow creation.");
            }

            state.TargetExists = true;
            Logger.LogTargetProvisioned(
                SinkKey,
                target.DestinationIdentity.ClientKey,
                target.DestinationIdentity.TargetName);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<ReplicaTargetInspection> InspectAsync(
        ReplicaTargetDescriptor target,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateClientKey(target.DestinationIdentity);
        Logger.LogReplicaInspected(
            SinkKey,
            target.DestinationIdentity.ClientKey,
            target.DestinationIdentity.TargetName);
        if (!Targets.TryGetValue(target.DestinationIdentity, out BootstrapReplicaTargetState? state))
        {
            return ValueTask.FromResult(new ReplicaTargetInspection(target.DestinationIdentity, false, 0));
        }

        lock (state.SyncRoot)
        {
            return ValueTask.FromResult(
                new ReplicaTargetInspection(
                    target.DestinationIdentity,
                    state.TargetExists,
                    state.WriteCount,
                    state.LatestSourcePosition,
                    state.LatestPayload));
        }
    }

    /// <inheritdoc />
    public ValueTask<ReplicaWriteResult> WriteAsync(
        ReplicaWriteRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateClientKey(request.Target.DestinationIdentity);
        if (!Targets.TryGetValue(request.Target.DestinationIdentity, out BootstrapReplicaTargetState? state))
        {
            throw new InvalidOperationException(
                $"Bootstrap replica sink '{SinkKey}' target '{request.Target.DestinationIdentity}' has not been provisioned.");
        }

        lock (state.SyncRoot)
        {
            if (!state.TargetExists)
            {
                throw new InvalidOperationException(
                    $"Bootstrap replica sink '{SinkKey}' target '{request.Target.DestinationIdentity}' has not been provisioned.");
            }

            if (!state.LatestSourcePosition.HasValue || (request.SourcePosition > state.LatestSourcePosition.Value))
            {
                state.LatestSourcePosition = request.SourcePosition;
                state.LatestPayload = request.Payload;
                state.WriteCount++;
                Logger.LogReplicaApplied(
                    SinkKey,
                    request.Target.DestinationIdentity.ClientKey,
                    request.Target.DestinationIdentity.TargetName,
                    request.SourcePosition);
                return ValueTask.FromResult(
                    new ReplicaWriteResult(
                        ReplicaWriteOutcome.Applied,
                        request.Target.DestinationIdentity,
                        request.SourcePosition));
            }

            if (request.SourcePosition == state.LatestSourcePosition.Value)
            {
                Logger.LogReplicaDuplicateIgnored(
                    SinkKey,
                    request.Target.DestinationIdentity.ClientKey,
                    request.Target.DestinationIdentity.TargetName,
                    request.SourcePosition);
                return ValueTask.FromResult(
                    new ReplicaWriteResult(
                        ReplicaWriteOutcome.DuplicateIgnored,
                        request.Target.DestinationIdentity,
                        request.SourcePosition));
            }

            Logger.LogReplicaSupersededIgnored(
                SinkKey,
                request.Target.DestinationIdentity.ClientKey,
                request.Target.DestinationIdentity.TargetName,
                request.SourcePosition);
            return ValueTask.FromResult(
                new ReplicaWriteResult(
                    ReplicaWriteOutcome.SupersededIgnored,
                    request.Target.DestinationIdentity,
                    request.SourcePosition));
        }
    }

    private void ValidateClientKey(
        ReplicaDestinationIdentity destinationIdentity
    )
    {
        if (!string.Equals(destinationIdentity.ClientKey, Options.ClientKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Bootstrap replica sink '{SinkKey}' was registered for client key '{Options.ClientKey}', but '{destinationIdentity.ClientKey}' was requested.");
        }
    }
}
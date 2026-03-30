using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Describes the outcome of a controlled dead-letter re-drive request.
/// </summary>
public sealed class ReplicaSinkDeadLetterReDriveResult
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkDeadLetterReDriveResult" /> class.
    /// </summary>
    public ReplicaSinkDeadLetterReDriveResult(
        string deliveryKey,
        string outcome,
        bool wasQueued,
        long? targetSourcePosition = null
    )
    {
        ArgumentNullException.ThrowIfNull(deliveryKey);
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        DeliveryKey = deliveryKey;
        Outcome = outcome;
        WasQueued = wasQueued;
        TargetSourcePosition = targetSourcePosition;
    }

    /// <summary>
    ///     Gets the runtime-owned delivery key.
    /// </summary>
    public string DeliveryKey { get; }

    /// <summary>
    ///     Gets the stable outcome string for the re-drive request.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    ///     Gets the source position that was queued for re-drive, when one was queued.
    /// </summary>
    public long? TargetSourcePosition { get; }

    /// <summary>
    ///     Gets a value indicating whether the runtime queued work for the request.
    /// </summary>
    public bool WasQueued { get; }
}

using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Describes a controlled dead-letter re-drive request.
/// </summary>
public sealed class ReplicaSinkDeadLetterReDriveRequest
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkDeadLetterReDriveRequest" /> class.
    /// </summary>
    /// <param name="context">The operator context for the request.</param>
    /// <param name="deliveryKey">The runtime-owned delivery key to re-drive.</param>
    public ReplicaSinkDeadLetterReDriveRequest(
        ReplicaSinkOperatorContext context,
        string deliveryKey
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(deliveryKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        Context = context;
        DeliveryKey = deliveryKey;
    }

    /// <summary>
    ///     Gets the operator context for the request.
    /// </summary>
    public ReplicaSinkOperatorContext Context { get; }

    /// <summary>
    ///     Gets the runtime-owned delivery key to re-drive.
    /// </summary>
    public string DeliveryKey { get; }
}

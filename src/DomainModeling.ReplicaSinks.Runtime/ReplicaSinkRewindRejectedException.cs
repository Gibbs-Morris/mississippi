using System;
using System.Globalization;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Raised when slice-1 latest-state processing is asked to rewind an existing delivery lane.
/// </summary>
internal sealed class ReplicaSinkRewindRejectedException : InvalidOperationException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkRewindRejectedException" /> class.
    /// </summary>
    public ReplicaSinkRewindRejectedException()
    {
        DeliveryKey = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkRewindRejectedException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ReplicaSinkRewindRejectedException(
        string message
    )
        : base(message)
    {
        DeliveryKey = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkRewindRejectedException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ReplicaSinkRewindRejectedException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        DeliveryKey = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkRewindRejectedException" /> class.
    /// </summary>
    /// <param name="deliveryKey">The runtime-owned delivery key.</param>
    /// <param name="requestedSourcePosition">The requested rewind source position.</param>
    /// <param name="desiredSourcePosition">The currently persisted desired source position, if any.</param>
    /// <param name="committedSourcePosition">The currently persisted committed source position, if any.</param>
    public ReplicaSinkRewindRejectedException(
        string deliveryKey,
        long requestedSourcePosition,
        long? desiredSourcePosition,
        long? committedSourcePosition
    )
        : base(
            string.Format(
                CultureInfo.InvariantCulture,
                "Replica sink delivery lane '{0}' cannot rewind from desired '{1}' / committed '{2}' to requested source position '{3}' in slice 1.",
                deliveryKey,
                desiredSourcePosition?.ToString(CultureInfo.InvariantCulture) ?? "<none>",
                committedSourcePosition?.ToString(CultureInfo.InvariantCulture) ?? "<none>",
                requestedSourcePosition))
    {
        DeliveryKey = deliveryKey;
        RequestedSourcePosition = requestedSourcePosition;
        DesiredSourcePosition = desiredSourcePosition;
        CommittedSourcePosition = committedSourcePosition;
    }

    /// <summary>
    ///     Gets the committed source position that the runtime had already checkpointed, if any.
    /// </summary>
    public long? CommittedSourcePosition { get; }

    /// <summary>
    ///     Gets the runtime-owned delivery key.
    /// </summary>
    public string DeliveryKey { get; }

    /// <summary>
    ///     Gets the desired source position that was already persisted, if any.
    /// </summary>
    public long? DesiredSourcePosition { get; }

    /// <summary>
    ///     Gets the requested rewind source position.
    /// </summary>
    public long RequestedSourcePosition { get; }
}

using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Represents the durable delivery state for a single replica sink delivery lane.
/// </summary>
public sealed class ReplicaSinkDeliveryState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkDeliveryState" /> class.
    /// </summary>
    /// <param name="deliveryKey">The runtime-owned delivery key.</param>
    /// <param name="desiredSourcePosition">The highest desired source position observed for the lane.</param>
    /// <param name="bootstrapUpperBoundSourcePosition">
    ///     The bootstrap cutover fence for the lane, when bootstrap work is still constrained below live traffic.
    /// </param>
    /// <param name="committedSourcePosition">The highest source position durably checkpointed as externally committed.</param>
    /// <param name="retry">The currently persisted retry state, if any.</param>
    /// <param name="deadLetter">The currently persisted dead-letter state, if any.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="deliveryKey" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="deliveryKey" /> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when any supplied position is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the supplied state is internally inconsistent.
    /// </exception>
    public ReplicaSinkDeliveryState(
        string deliveryKey,
        long? desiredSourcePosition = null,
        long? bootstrapUpperBoundSourcePosition = null,
        long? committedSourcePosition = null,
        ReplicaSinkStoredFailure? retry = null,
        ReplicaSinkStoredFailure? deadLetter = null
    )
    {
        ArgumentNullException.ThrowIfNull(deliveryKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        ValidatePosition(desiredSourcePosition, nameof(desiredSourcePosition));
        ValidatePosition(bootstrapUpperBoundSourcePosition, nameof(bootstrapUpperBoundSourcePosition));
        ValidatePosition(committedSourcePosition, nameof(committedSourcePosition));
        if (desiredSourcePosition is not null &&
            committedSourcePosition is not null &&
            (desiredSourcePosition.Value < committedSourcePosition.Value))
        {
            throw new InvalidOperationException(
                "Desired source position cannot be older than the committed source position.");
        }

        if (bootstrapUpperBoundSourcePosition is not null &&
            desiredSourcePosition is not null &&
            (bootstrapUpperBoundSourcePosition.Value > desiredSourcePosition.Value))
        {
            throw new InvalidOperationException(
                "Bootstrap upper-bound source position cannot be newer than the desired source position.");
        }

        if (retry is not null && deadLetter is not null)
        {
            throw new InvalidOperationException(
                "Retry and dead-letter state cannot both be active for the same delivery lane.");
        }

        if ((retry is not null || deadLetter is not null) && desiredSourcePosition is null)
        {
            throw new InvalidOperationException("Failure state cannot exist without a desired source position.");
        }

        if (retry is not null &&
            desiredSourcePosition is not null &&
            (retry.SourcePosition > desiredSourcePosition.Value))
        {
            throw new InvalidOperationException(
                "Retry source position cannot be newer than the desired source position.");
        }

        if (deadLetter is not null &&
            desiredSourcePosition is not null &&
            (deadLetter.SourcePosition > desiredSourcePosition.Value))
        {
            throw new InvalidOperationException(
                "Dead-letter source position cannot be newer than the desired source position.");
        }

        DeliveryKey = deliveryKey;
        DesiredSourcePosition = desiredSourcePosition;
        BootstrapUpperBoundSourcePosition = bootstrapUpperBoundSourcePosition;
        CommittedSourcePosition = committedSourcePosition;
        Retry = retry;
        DeadLetter = deadLetter;
    }

    /// <summary>
    ///     Gets the bootstrap cutover fence for the lane, when bootstrap work is still constrained below live traffic.
    /// </summary>
    public long? BootstrapUpperBoundSourcePosition { get; }

    /// <summary>
    ///     Gets the highest source position durably checkpointed as externally committed.
    /// </summary>
    public long? CommittedSourcePosition { get; }

    /// <summary>
    ///     Gets the currently persisted dead-letter state, if any.
    /// </summary>
    public ReplicaSinkStoredFailure? DeadLetter { get; }

    /// <summary>
    ///     Gets the runtime-owned delivery key.
    /// </summary>
    public string DeliveryKey { get; }

    /// <summary>
    ///     Gets the highest desired source position observed for the lane.
    /// </summary>
    public long? DesiredSourcePosition { get; }

    /// <summary>
    ///     Gets the currently persisted retry state, if any.
    /// </summary>
    public ReplicaSinkStoredFailure? Retry { get; }

    private static void ValidatePosition(
        long? position,
        string paramName
    )
    {
        if (position is not null && (position.Value < 0))
        {
            throw new ArgumentOutOfRangeException(paramName, "Replica sink source positions cannot be negative.");
        }
    }
}
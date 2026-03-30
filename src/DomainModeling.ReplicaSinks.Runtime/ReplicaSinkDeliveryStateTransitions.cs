using System;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Provides shared transition rules for durable replica sink delivery state.
/// </summary>
internal static class ReplicaSinkDeliveryStateTransitions
{
    public static ReplicaSinkDeliveryState AdvanceDesiredPosition(
        ReplicaSinkDeliveryState currentState,
        long desiredSourcePosition,
        ReplicaSinkDeliveryIdentity deliveryIdentity,
        long? bootstrapUpperBoundSourcePosition = null
    )
    {
        ArgumentNullException.ThrowIfNull(currentState);
        ArgumentNullException.ThrowIfNull(deliveryIdentity.EntityId);
        ArgumentOutOfRangeException.ThrowIfNegative(desiredSourcePosition);
        long? currentDesiredSourcePosition = currentState.DesiredSourcePosition;
        long? committedSourcePosition = currentState.CommittedSourcePosition;
        if ((currentDesiredSourcePosition is not null &&
             (desiredSourcePosition < currentDesiredSourcePosition.Value)) ||
            (committedSourcePosition is not null && (desiredSourcePosition < committedSourcePosition.Value)))
        {
            throw new ReplicaSinkRewindRejectedException(
                deliveryIdentity.DeliveryKey,
                desiredSourcePosition,
                currentDesiredSourcePosition,
                committedSourcePosition);
        }

        long? effectiveBootstrapUpperBound = currentState.BootstrapUpperBoundSourcePosition;
        if (bootstrapUpperBoundSourcePosition is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bootstrapUpperBoundSourcePosition.Value);
            if (bootstrapUpperBoundSourcePosition.Value > desiredSourcePosition)
            {
                throw new InvalidOperationException(
                    "Bootstrap upper-bound source position cannot be newer than the desired source position.");
            }

            effectiveBootstrapUpperBound = effectiveBootstrapUpperBound is null
                ? bootstrapUpperBoundSourcePosition
                : Math.Max(effectiveBootstrapUpperBound.Value, bootstrapUpperBoundSourcePosition.Value);
        }

        long supersessionThreshold = desiredSourcePosition;
        if (effectiveBootstrapUpperBound is not null &&
            (committedSourcePosition is null || committedSourcePosition.Value < effectiveBootstrapUpperBound.Value))
        {
            supersessionThreshold = Math.Min(desiredSourcePosition, effectiveBootstrapUpperBound.Value);
        }

        ReplicaSinkStoredFailure? retry = currentState.Retry;
        ReplicaSinkStoredFailure? deadLetter = currentState.DeadLetter;
        if (retry is not null && (retry.SourcePosition < supersessionThreshold))
        {
            retry = null;
        }

        if (deadLetter is not null && (deadLetter.SourcePosition < supersessionThreshold))
        {
            deadLetter = null;
        }

        return new(
            currentState.DeliveryKey,
            desiredSourcePosition,
            effectiveBootstrapUpperBound,
            currentState.CommittedSourcePosition,
            retry,
            deadLetter);
    }

    public static ReplicaSinkDeliveryState ClearDeadLetter(
        ReplicaSinkDeliveryState currentState
    )
    {
        ArgumentNullException.ThrowIfNull(currentState);
        return new(
            currentState.DeliveryKey,
            currentState.DesiredSourcePosition,
            currentState.BootstrapUpperBoundSourcePosition,
            currentState.CommittedSourcePosition,
            currentState.Retry,
            null);
    }

    public static ReplicaSinkDeliveryState CreateCommittedState(
        ReplicaSinkDeliveryState currentState,
        long sourcePosition
    )
    {
        ArgumentNullException.ThrowIfNull(currentState);
        ArgumentOutOfRangeException.ThrowIfNegative(sourcePosition);
        long? bootstrapUpperBoundSourcePosition = currentState.BootstrapUpperBoundSourcePosition;
        if (bootstrapUpperBoundSourcePosition is not null && sourcePosition >= bootstrapUpperBoundSourcePosition.Value)
        {
            bootstrapUpperBoundSourcePosition = null;
        }

        return new(
            currentState.DeliveryKey,
            currentState.DesiredSourcePosition,
            bootstrapUpperBoundSourcePosition,
            sourcePosition);
    }

    public static long? GetEffectiveTargetSourcePosition(
        ReplicaSinkDeliveryState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.DesiredSourcePosition is null)
        {
            return null;
        }

        if (state.BootstrapUpperBoundSourcePosition is not null &&
            (state.CommittedSourcePosition is null ||
             state.CommittedSourcePosition.Value < state.BootstrapUpperBoundSourcePosition.Value))
        {
            return Math.Min(state.DesiredSourcePosition.Value, state.BootstrapUpperBoundSourcePosition.Value);
        }

        return state.DesiredSourcePosition;
    }

    public static bool HasProcessableWork(
        ReplicaSinkDeliveryState state,
        DateTimeOffset now
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        long? targetSourcePosition = GetEffectiveTargetSourcePosition(state);
        if (targetSourcePosition is null)
        {
            return false;
        }

        if (state.CommittedSourcePosition is not null && state.CommittedSourcePosition.Value >= targetSourcePosition.Value)
        {
            return false;
        }

        if (state.DeadLetter?.SourcePosition == targetSourcePosition.Value)
        {
            return false;
        }

        return state.Retry is null ||
               state.Retry.SourcePosition != targetSourcePosition.Value ||
               state.Retry.NextRetryAtUtc is null ||
               state.Retry.NextRetryAtUtc.Value <= now;
    }
}

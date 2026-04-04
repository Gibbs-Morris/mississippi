namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Helper methods for producing normalized checkpoint state transitions.
/// </summary>
internal static class SagaRecoveryCheckpointReducerState
{
    /// <summary>
    ///     Clears the in-flight attempt metadata and cached pending step name while preserving the pending direction
    ///     and step index.
    /// </summary>
    /// <param name="state">The current checkpoint state.</param>
    /// <returns>The updated checkpoint with in-flight metadata removed.</returns>
    public static SagaRecoveryCheckpoint ClearInFlight(
        SagaRecoveryCheckpoint state
    ) =>
        state with
        {
            InFlightAttemptId = null,
            InFlightOperationKey = null,
            PendingStepName = null,
        };

    /// <summary>
    ///     Clears pending work and in-flight metadata when the saga becomes terminal.
    /// </summary>
    /// <param name="state">The current checkpoint state.</param>
    /// <returns>The updated checkpoint with pending recovery state removed.</returns>
    public static SagaRecoveryCheckpoint ClearPending(
        SagaRecoveryCheckpoint state
    ) =>
        state with
        {
            PendingDirection = null,
            PendingStepIndex = null,
            PendingStepName = null,
            InFlightAttemptId = null,
            InFlightOperationKey = null,
            BlockedReason = null,
            NextEligibleResumeAt = null,
            ReminderArmed = false,
        };
}
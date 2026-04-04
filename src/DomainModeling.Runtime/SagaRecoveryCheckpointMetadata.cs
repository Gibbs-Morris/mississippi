using System;
using System.Security.Cryptography;
using System.Text;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Provides the authoritative recovery-checkpoint reducer hash used to scope checkpoint snapshots.
/// </summary>
internal static class SagaRecoveryCheckpointMetadata
{
    /// <summary>
    ///     Gets the reducer hash for framework-owned saga recovery checkpoints.
    /// </summary>
    internal static string CheckpointReducerHash { get; } = ComputeCheckpointReducerHash();

    private static string ComputeCheckpointReducerHash()
    {
        string[] reducerTypeNames =
        [
            typeof(SagaRecoveryCheckpointStartedReducer).FullName ?? nameof(SagaRecoveryCheckpointStartedReducer),
            typeof(SagaRecoveryCheckpointExecutionStartedReducer).FullName ??
            nameof(SagaRecoveryCheckpointExecutionStartedReducer),
            typeof(SagaRecoveryCheckpointResumeBlockedReducer).FullName ??
            nameof(SagaRecoveryCheckpointResumeBlockedReducer),
            typeof(SagaRecoveryCheckpointStepCompletedReducer).FullName ??
            nameof(SagaRecoveryCheckpointStepCompletedReducer),
            typeof(SagaRecoveryCheckpointStepFailedReducer).FullName ?? nameof(SagaRecoveryCheckpointStepFailedReducer),
            typeof(SagaRecoveryCheckpointCompensatingReducer).FullName ??
            nameof(SagaRecoveryCheckpointCompensatingReducer),
            typeof(SagaRecoveryCheckpointStepCompensatedReducer).FullName ??
            nameof(SagaRecoveryCheckpointStepCompensatedReducer),
            typeof(SagaRecoveryCheckpointCompletedReducer).FullName ?? nameof(SagaRecoveryCheckpointCompletedReducer),
            typeof(SagaRecoveryCheckpointCompensatedReducer).FullName ??
            nameof(SagaRecoveryCheckpointCompensatedReducer),
            typeof(SagaRecoveryCheckpointFailedReducer).FullName ?? nameof(SagaRecoveryCheckpointFailedReducer),
        ];
        Array.Sort(reducerTypeNames, StringComparer.Ordinal);
        string input = string.Join("|", reducerTypeNames);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
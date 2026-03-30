namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Describes how the runtime should persist a provider write failure.
/// </summary>
public enum ReplicaSinkWriteFailureDisposition
{
    /// <summary>
    ///     Persist the failure as retry state.
    /// </summary>
    Retry,

    /// <summary>
    ///     Persist the failure as a dead letter.
    /// </summary>
    DeadLetter,
}
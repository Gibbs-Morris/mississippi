namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Describes the terminal outcome of a replica write attempt.
/// </summary>
public enum ReplicaWriteOutcome
{
    /// <summary>
    ///     The write was applied to the target.
    /// </summary>
    Applied = 0,

    /// <summary>
    ///     The write matched an already-applied source position.
    /// </summary>
    DuplicateIgnored = 1,

    /// <summary>
    ///     The write was older than the latest applied source position.
    /// </summary>
    SupersededIgnored = 2,
}
namespace Mississippi.DomainModeling.ReplicaSinks.Abstractions;

/// <summary>
///     Defines the persistence strategy requested for a replica sink binding.
/// </summary>
public enum ReplicaWriteMode
{
    /// <summary>
    ///     Persists only the latest state for each delivery key.
    /// </summary>
    LatestState = 0,

    /// <summary>
    ///     Persists a history item for each source position.
    /// </summary>
    /// <remarks>
    ///     Slice 1 exposes this value at the contract level but startup validation rejects it for runnable paths.
    /// </remarks>
    History = 1,
}
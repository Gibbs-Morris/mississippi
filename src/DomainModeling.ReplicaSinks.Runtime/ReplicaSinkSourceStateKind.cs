namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Describes the latest-state source semantics read from the projection pipeline.
/// </summary>
internal enum ReplicaSinkSourceStateKind
{
    /// <summary>
    ///     A concrete source value exists and should be materialized.
    /// </summary>
    Value,

    /// <summary>
    ///     The source has been explicitly deleted or tombstoned.
    /// </summary>
    Deleted,

    /// <summary>
    ///     The source is absent and should be represented as a delete-like outbound write.
    /// </summary>
    Absent,
}
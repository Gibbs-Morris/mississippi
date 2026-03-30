namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;

/// <summary>
///     Stores the mutable in-memory state for a bootstrap replica target.
/// </summary>
internal sealed class BootstrapReplicaTargetState
{
    /// <summary>
    ///     Gets or sets the latest applied payload.
    /// </summary>
    public object? LatestPayload { get; set; }

    /// <summary>
    ///     Gets or sets the latest applied source position.
    /// </summary>
    public long? LatestSourcePosition { get; set; }

    /// <summary>
    ///     Gets an object used to synchronize state updates.
    /// </summary>
    public object SyncRoot { get; } = new();

    /// <summary>
    ///     Gets or sets a value indicating whether the target exists.
    /// </summary>
    public bool TargetExists { get; set; }

    /// <summary>
    ///     Gets or sets the number of applied writes.
    /// </summary>
    public long WriteCount { get; set; }
}
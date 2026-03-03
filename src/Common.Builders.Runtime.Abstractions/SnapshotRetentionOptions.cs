namespace Mississippi.Common.Builders.Runtime.Abstractions;

/// <summary>
///     Options for snapshot retention policy configuration.
/// </summary>
public sealed class SnapshotRetentionOptions
{
    /// <summary>
    ///     Gets or sets the maximum number of snapshots to retain.
    /// </summary>
    public int MaxSnapshotsToRetain { get; set; }

    /// <summary>
    ///     Gets or sets the number of events between snapshot writes.
    /// </summary>
    public int SnapshotEveryNEvents { get; set; }
}
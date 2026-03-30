using System;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Represents a single projection-to-sink binding discovered from <see cref="ProjectionReplicationAttribute" />.
/// </summary>
internal sealed class ReplicaSinkProjectionDescriptor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkProjectionDescriptor" /> class.
    /// </summary>
    /// <param name="projectionType">The projection type declaring the binding.</param>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="targetName">The provider-neutral destination name.</param>
    /// <param name="contractType">The mapped contract type, when present.</param>
    /// <param name="isDirectProjectionReplicationEnabled">
    ///     A value indicating whether direct projection replication is
    ///     explicitly enabled.
    /// </param>
    /// <param name="writeMode">The requested write mode.</param>
    public ReplicaSinkProjectionDescriptor(
        Type projectionType,
        string sinkKey,
        string targetName,
        Type? contractType,
        bool isDirectProjectionReplicationEnabled,
        ReplicaWriteMode writeMode
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentNullException.ThrowIfNull(targetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ProjectionType = projectionType;
        SinkKey = sinkKey;
        TargetName = targetName;
        ContractType = contractType;
        IsDirectProjectionReplicationEnabled = isDirectProjectionReplicationEnabled;
        WriteMode = writeMode;
    }

    /// <summary>
    ///     Gets the mapped contract type, when present.
    /// </summary>
    public Type? ContractType { get; }

    /// <summary>
    ///     Gets a value indicating whether direct projection replication is explicitly enabled.
    /// </summary>
    public bool IsDirectProjectionReplicationEnabled { get; }

    /// <summary>
    ///     Gets the projection type declaring the binding.
    /// </summary>
    public Type ProjectionType { get; }

    /// <summary>
    ///     Gets the named sink registration key.
    /// </summary>
    public string SinkKey { get; }

    /// <summary>
    ///     Gets the provider-neutral destination name.
    /// </summary>
    public string TargetName { get; }

    /// <summary>
    ///     Gets the requested write mode.
    /// </summary>
    public ReplicaWriteMode WriteMode { get; }
}
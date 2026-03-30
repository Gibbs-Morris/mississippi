using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Identifies a logical projection-to-sink binding independently of provider-specific destination identity.
/// </summary>
internal sealed class ReplicaSinkBindingIdentity : IEquatable<ReplicaSinkBindingIdentity>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkBindingIdentity" /> class.
    /// </summary>
    /// <param name="projectionTypeName">The projection type name.</param>
    /// <param name="sinkKey">The named sink key.</param>
    /// <param name="targetName">The provider-neutral target name.</param>
    public ReplicaSinkBindingIdentity(
        string projectionTypeName,
        string sinkKey,
        string targetName
    )
    {
        ArgumentNullException.ThrowIfNull(projectionTypeName);
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentNullException.ThrowIfNull(targetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ProjectionTypeName = projectionTypeName;
        SinkKey = sinkKey;
        TargetName = targetName;
    }

    /// <summary>
    ///     Gets the projection type name.
    /// </summary>
    public string ProjectionTypeName { get; }

    /// <summary>
    ///     Gets the named sink key.
    /// </summary>
    public string SinkKey { get; }

    /// <summary>
    ///     Gets the provider-neutral target name.
    /// </summary>
    public string TargetName { get; }

    /// <inheritdoc />
    public bool Equals(
        ReplicaSinkBindingIdentity? other
    ) =>
        other is not null &&
        string.Equals(ProjectionTypeName, other.ProjectionTypeName, StringComparison.Ordinal) &&
        string.Equals(SinkKey, other.SinkKey, StringComparison.Ordinal) &&
        string.Equals(TargetName, other.TargetName, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(
        object? obj
    ) =>
        obj is ReplicaSinkBindingIdentity other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(ProjectionTypeName, SinkKey, TargetName);

    /// <inheritdoc />
    public override string ToString() => $"{ProjectionTypeName}|{SinkKey}|{TargetName}";
}
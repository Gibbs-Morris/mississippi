using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Abstractions;

/// <summary>
///     Declares that a projection can replicate latest-state data to a named replica sink.
/// </summary>
/// <remarks>
///     <para>
///         Mapped replica contracts are the default path. When <see cref="ContractType" /> is omitted,
///         direct projection replication remains disabled unless
///         <see cref="IsDirectProjectionReplicationEnabled" /> is set to <see langword="true" /> and the
///         projection type declares <see cref="ReplicaContractNameAttribute" /> so the runtime can compute a
///         stable contract identity.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ProjectionReplicationAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionReplicationAttribute" /> class for direct projection
    ///     replication.
    /// </summary>
    /// <param name="sink">The named sink registration key.</param>
    /// <param name="targetName">The provider-neutral destination name.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sink" /> or <paramref name="targetName" /> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="sink" /> or <paramref name="targetName" /> is empty or
    ///     whitespace.
    /// </exception>
    public ProjectionReplicationAttribute(
        string sink,
        string targetName
    )
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(targetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sink);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        Sink = sink;
        TargetName = targetName;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionReplicationAttribute" /> class for mapped replica contracts.
    /// </summary>
    /// <param name="sink">The named sink registration key.</param>
    /// <param name="targetName">The provider-neutral destination name.</param>
    /// <param name="contractType">The replica contract type written to the sink.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="sink" />, <paramref name="targetName" />, or <paramref name="contractType" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="sink" /> or <paramref name="targetName" /> is empty or
    ///     whitespace.
    /// </exception>
    public ProjectionReplicationAttribute(
        string sink,
        string targetName,
        Type contractType
    )
        : this(sink, targetName)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        ContractType = contractType;
    }

    /// <summary>
    ///     Gets the replica contract type used for mapped replication.
    /// </summary>
    public Type? ContractType { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether direct projection replication is explicitly enabled.
    /// </summary>
    public bool IsDirectProjectionReplicationEnabled { get; set; }

    /// <summary>
    ///     Gets the named sink registration key.
    /// </summary>
    public string Sink { get; }

    /// <summary>
    ///     Gets the provider-neutral destination name.
    /// </summary>
    public string TargetName { get; }

    /// <summary>
    ///     Gets or sets the requested write mode.
    /// </summary>
    public ReplicaWriteMode WriteMode { get; set; } = ReplicaWriteMode.LatestState;
}
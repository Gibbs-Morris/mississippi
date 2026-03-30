using System;
using System.Collections.Generic;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Stores the replica sink projection bindings discovered during startup.
/// </summary>
internal sealed class ReplicaSinkProjectionRegistry : IReplicaSinkProjectionRegistry
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkProjectionRegistry" /> class.
    /// </summary>
    /// <param name="projectionBindings">The discovered projection bindings.</param>
    public ReplicaSinkProjectionRegistry(
        IReadOnlyList<ReplicaSinkProjectionDescriptor>? projectionBindings = null
    ) =>
        ProjectionBindings = projectionBindings ?? Array.Empty<ReplicaSinkProjectionDescriptor>();

    private IReadOnlyList<ReplicaSinkProjectionDescriptor> ProjectionBindings { get; }

    /// <inheritdoc />
    public IReadOnlyList<ReplicaSinkProjectionDescriptor> GetProjectionBindings() => ProjectionBindings;
}
using System.Collections.Generic;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Exposes the projection bindings discovered for replica sink onboarding.
/// </summary>
internal interface IReplicaSinkProjectionRegistry
{
    /// <summary>
    ///     Gets the discovered projection bindings.
    /// </summary>
    /// <returns>The discovered projection bindings.</returns>
    IReadOnlyList<ReplicaSinkProjectionDescriptor> GetProjectionBindings();
}
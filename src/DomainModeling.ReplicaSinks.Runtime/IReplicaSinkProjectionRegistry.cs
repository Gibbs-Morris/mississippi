using System.Collections.Generic;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Exposes the projection bindings discovered for replica sink onboarding.
/// </summary>
internal interface IReplicaSinkProjectionRegistry
{
    /// <summary>
    ///     Gets the cached runtime binding descriptors.
    /// </summary>
    /// <returns>The cached runtime binding descriptors.</returns>
    IReadOnlyList<ReplicaSinkBindingDescriptor> GetBindingDescriptors();

    /// <summary>
    ///     Gets the cached startup validation diagnostics.
    /// </summary>
    /// <returns>The cached startup validation diagnostics.</returns>
    IReadOnlyList<ReplicaSinkStartupDiagnostic> GetDiagnostics();
}
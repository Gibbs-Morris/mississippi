using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Registers a snapshot type with the snapshot type registry during service activation.
/// </summary>
internal interface ISnapshotTypeRegistration
{
    /// <summary>
    ///     Registers the snapshot type with the registry.
    /// </summary>
    /// <param name="registry">The snapshot type registry.</param>
    void Register(
        ISnapshotTypeRegistry registry
    );
}
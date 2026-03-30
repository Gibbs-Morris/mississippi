namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Represents a provider capable of provisioning, writing, and inspecting replica sink targets.
/// </summary>
public interface IReplicaSinkProvider
    : IReplicaSinkProvisioner,
      IReplicaSinkWriter,
      IReplicaSinkInspector
{
    /// <summary>
    ///     Gets the informational provider format identifier.
    /// </summary>
    string Format { get; }
}
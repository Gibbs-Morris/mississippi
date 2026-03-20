using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Registers a single snapshot type with the snapshot type registry.
/// </summary>
/// <typeparam name="TSnapshot">The snapshot type to register.</typeparam>
internal sealed class SnapshotTypeRegistration<TSnapshot> : ISnapshotTypeRegistration
    where TSnapshot : class
{
    /// <inheritdoc />
    public void Register(
        ISnapshotTypeRegistry registry
    )
    {
        string snapshotName = SnapshotStorageNameHelper.GetStorageName<TSnapshot>();
        registry.Register(snapshotName, typeof(TSnapshot));
    }
}
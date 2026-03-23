namespace Mississippi.Tributary.Runtime.L0Tests;

/// <summary>
///     Primary serializer type for snapshot-converter tests.
/// </summary>
internal sealed class PrimaryTestSnapshotSerializationProvider : TestSnapshotSerializationProviderBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PrimaryTestSnapshotSerializationProvider" /> class.
    /// </summary>
    /// <param name="format">The serializer content type.</param>
    public PrimaryTestSnapshotSerializationProvider(
        string format
    )
        : base(format)
    {
    }
}
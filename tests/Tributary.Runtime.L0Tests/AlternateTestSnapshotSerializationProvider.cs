namespace Mississippi.Tributary.Runtime.L0Tests;

/// <summary>
///     Alternate serializer type for snapshot-converter tests.
/// </summary>
internal sealed class AlternateTestSnapshotSerializationProvider : TestSnapshotSerializationProviderBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AlternateTestSnapshotSerializationProvider" /> class.
    /// </summary>
    /// <param name="format">The serializer content type.</param>
    public AlternateTestSnapshotSerializationProvider(
        string format
    )
        : base(format)
    {
    }
}
namespace Mississippi.Core.Streams.StorageProvider;

/// <summary>
///     This is what someone needs to implement to write the event somewhere.
/// </summary>
public interface IStreamStorageProvider
    : IStreamStorageReader,
      IStreamStorageWriter
{
}
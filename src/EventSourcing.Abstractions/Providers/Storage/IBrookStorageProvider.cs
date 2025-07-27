namespace Mississippi.Core.Abstractions.Providers.Storage;

/// <summary>
///     This is what someone needs to implement to write the event somewhere.
/// </summary>
public interface IBrookStorageProvider
    : IBrookStorageReader,
      IBrookStorageWriter
{
    /// <summary>
    ///     Gets the format identifier for this brook storage provider.
    /// </summary>
    string Format { get; }
}
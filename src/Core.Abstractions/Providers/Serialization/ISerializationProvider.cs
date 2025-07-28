namespace Mississippi.Core.Abstractions.Providers.Serialization;

/// <summary>
///     Provides unified access to serialization operations combining read, write, and async operations.
/// </summary>
public interface ISerializationProvider
    : ISerializationReader,
      IAsyncSerializationReader,
      ISerializationWriter,
      IAsyncSerializationWriter
{
    /// <summary>
    ///     Gets the canonical name for logging and routing purposes (e.g., "json", "avro", "protobuf").
    /// </summary>
    /// <value>A string representing the format name of this serialization provider.</value>
    string Format { get; }
}
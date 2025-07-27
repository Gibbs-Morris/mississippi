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
    /// Canonical name for logging / routing (“json”, “avro”, “protobuf”…).
    string Format { get; }
}
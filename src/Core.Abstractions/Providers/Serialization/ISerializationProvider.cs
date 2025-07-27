namespace Mississippi.Core.Abstractions.Providers.Serialization;

public interface ISerializationProvider :
    ISerializationReader,
    IAsyncSerializationReader,
    ISerializationWriter,
    IAsyncSerializationWriter
{
    /// Canonical name for logging / routing (“json”, “avro”, “protobuf”…).
    string Format { get; }
}
namespace Mississippi.Core.Abstractions.Providers.Serialization;

public interface ISerializationReader
{
    /// Pure, in-memory deserialisation. Never blocks on I/O.
    T Read<T>(ReadOnlyMemory<byte> payload);
}
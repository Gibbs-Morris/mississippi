namespace Mississippi.Core.Abstractions.Providers.Serialization;

public interface ISerializationWriter
{
    /// Pure, in-memory serialisation. Never blocks on I/O.
    ReadOnlyMemory<byte> Write<T>(T value);
}
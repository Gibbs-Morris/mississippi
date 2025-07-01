using System.Collections.Immutable;


namespace Mississippi.Core.Idea.EventSerialization;

public interface IDeserializer
{
    T Deserialize<T>(ImmutableArray<byte> bytes);
}
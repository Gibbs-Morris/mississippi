using System.Collections.Immutable;


namespace Mississippi.Core.Idea.EventSerialization;

public class JsonIDeserializer : IDeserializer
{
    public T Deserialize<T>(
        ImmutableArray<byte> bytes
    )
    {
        // use system.text.json
        // utf 8 stream
        throw new NotImplementedException();
    }
}
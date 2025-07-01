using System.Collections.Immutable;


namespace Mississippi.Core.Idea.EventSerialization;

public class JsonSerialize : ISerialize
{
    public ImmutableArray<byte> Serialize<T>(
        T objectToSerialize
    )
    {
        // use system.text.json
        // utf 8 stream
        throw new NotImplementedException();
    }
}
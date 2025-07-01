using System.Collections.Immutable;


namespace Mississippi.Core.Idea.EventSerialization;

public interface ISerialize
{
    ImmutableArray<byte> Serialize<T>(
        T objectToSerialize
    );
}
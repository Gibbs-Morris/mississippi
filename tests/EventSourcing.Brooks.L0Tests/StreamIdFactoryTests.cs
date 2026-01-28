
using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Brooks.L0Tests;

/// <summary>
///     Tests for <see cref="StreamIdFactory" /> behavior.
/// </summary>
public sealed class StreamIdFactoryTests
{
    /// <summary>
    ///     Verifies that creating a StreamId for the same BrookKey is deterministic.
    /// </summary>
    [Fact]
    public void CreateIsDeterministicForSameBrookKey()
    {
        StreamIdFactory factory = new();
        BrookKey key = new("type", "id");
        StreamId first = factory.Create(key);
        StreamId second = factory.Create(key);
        Assert.Equal(first, second);
    }

    /// <summary>
    ///     Verifies the factory matches a direct StreamId.Create call using the configured stream name.
    /// </summary>
    [Fact]
    public void CreateMatchesDirectStreamIdCreateCall()
    {
        StreamIdFactory factory = new();
        BrookKey key = new("typeX", "idX");
        StreamId expected = StreamId.Create(EventSourcingOrleansStreamNames.CursorUpdateStreamName, key);
        StreamId actual = factory.Create(key);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     Verifies that different BrookKeys produce different StreamIds.
    /// </summary>
    [Fact]
    public void CreateProducesDifferentStreamIdsForDifferentKeys()
    {
        StreamIdFactory factory = new();
        BrookKey a = new("type", "idA");
        BrookKey b = new("type", "idB");
        StreamId sa = factory.Create(a);
        StreamId sb = factory.Create(b);
        Assert.NotEqual(sa, sb);
    }
}
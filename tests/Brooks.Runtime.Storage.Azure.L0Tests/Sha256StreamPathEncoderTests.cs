using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Tests for <see cref="Sha256StreamPathEncoder" />.
/// </summary>
public sealed class Sha256StreamPathEncoderTests
{
    /// <summary>
    ///     The same brook key produces stable deterministic paths.
    /// </summary>
    [Fact]
    public void PathsAreDeterministicForTheSameBrook()
    {
        Sha256StreamPathEncoder encoder = new();
        BrookKey brookId = new("orders", "123");

        string cursorPath = encoder.GetCursorPath(brookId);
        string pendingPath = encoder.GetPendingPath(brookId);
        string eventPath = encoder.GetEventPath(brookId, 42);
        string lockPath = encoder.GetLockPath(brookId);

        Assert.Equal(cursorPath, encoder.GetCursorPath(brookId));
        Assert.Equal(pendingPath, encoder.GetPendingPath(brookId));
        Assert.Equal(eventPath, encoder.GetEventPath(brookId, 42));
        Assert.Equal(lockPath, encoder.GetLockPath(brookId));
    }

    /// <summary>
    ///     Different brook keys produce different deterministic paths.
    /// </summary>
    [Fact]
    public void PathsDifferForDifferentBrooks()
    {
        Sha256StreamPathEncoder encoder = new();

        Assert.NotEqual(
            encoder.GetCursorPath(new BrookKey("orders", "123")),
            encoder.GetCursorPath(new BrookKey("orders", "124")));
    }
}
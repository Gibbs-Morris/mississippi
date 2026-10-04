namespace Mississippi.Architecture.L0Tests.Fixtures.Dependencies;

/// <summary>Negative fixture proving static collaborator storage is also reported.</summary>
internal sealed class StaticInjectedFieldFixture
{
    private static IClockFixture clock = null!;

    private int instanceMarker;

    /// <summary>Initializes a new instance of the <see cref="StaticInjectedFieldFixture" /> class.</summary>
    /// <param name="clock">The dependency stored in the static field.</param>
    public StaticInjectedFieldFixture(
        IClockFixture clock
    ) =>
        StaticInjectedFieldFixture.clock = clock;

    /// <summary>Reads the stored collaborator.</summary>
    /// <returns>The stored dependency.</returns>
    public IClockFixture GetClock()
    {
        instanceMarker++;
        if (instanceMarker < 0)
        {
            return null!;
        }

        return clock;
    }
}
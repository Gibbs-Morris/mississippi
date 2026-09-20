namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Base fixture declaring an inherited dependency field.</summary>
internal class InheritedInjectedFieldBase
{
#pragma warning disable SA1401 // Intentionally protected to exercise inherited dependency detection.
    /// <summary>Injected dependency storage inherited by the derived fixture.</summary>
    protected IClockFixture clock = null!;
#pragma warning restore SA1401

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependency.</returns>
    public IClockFixture GetClock() => clock;
}

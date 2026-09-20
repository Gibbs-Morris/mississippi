namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Base fixture declaring an inherited dependency property backing field.</summary>
internal class InheritedInjectedFieldBase
{
    /// <summary>Gets or sets the injected dependency inherited by the derived fixture.</summary>
    protected IClockFixture Clock { get; set; } = null!;

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependency.</returns>
    public IClockFixture GetClock() => Clock;
}

namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Negative fixture whose manual property setter stores into a dependency field.</summary>
internal sealed class ManualFieldBackedInjectedPropertyFixture
{
    private IClockFixture clock = null!;

    /// <summary>Initializes a new instance of the <see cref="ManualFieldBackedInjectedPropertyFixture" /> class.</summary>
    /// <param name="clock">Dependency stored through the manual property setter.</param>
    public ManualFieldBackedInjectedPropertyFixture(IClockFixture clock) => Clock = clock;

    /// <summary>Gets or sets the injected dependency through a normal backing field.</summary>
#pragma warning disable S2292 // This fixture intentionally exercises a manual backing-field property.
    internal IClockFixture Clock
    {
        get => clock;
        set => clock = value;
    }
#pragma warning restore S2292

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependency.</returns>
    public IClockFixture GetClock() => clock;
}

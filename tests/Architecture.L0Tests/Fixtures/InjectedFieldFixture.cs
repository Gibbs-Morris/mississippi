namespace Mississippi.Architecture.L0Tests;

/// <summary>Negative fixture that stores an injected dependency in a field.</summary>
internal sealed class InjectedFieldFixture
{
    private readonly IClockFixture clock;

    /// <summary>Initializes a new instance of the <see cref="InjectedFieldFixture"/> class.</summary>
    /// <param name="clock">Dependency captured by the negative fixture.</param>
    public InjectedFieldFixture(IClockFixture clock) => this.clock = clock;

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependency.</returns>
    public IClockFixture GetClock() => clock;
}

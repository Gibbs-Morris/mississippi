using System.Collections.Generic;


namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Negative fixture that stores an injected dependency through a manual setter.</summary>
internal sealed class ManualSettableInjectedPropertyFixture
{
    private readonly Dictionary<string, IClockFixture> values = new();

    /// <summary>Initializes a new instance of the <see cref="ManualSettableInjectedPropertyFixture" /> class.</summary>
    /// <param name="clock">Dependency stored by the manual setter.</param>
    public ManualSettableInjectedPropertyFixture(
        IClockFixture clock
    ) =>
        Clock = clock;

    /// <summary>Gets or sets the injected dependency through a manual setter.</summary>
    internal IClockFixture Clock
    {
        get => values["clock"];
        set => values["clock"] = value;
    }

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependency.</returns>
    public IClockFixture GetClock() => values["clock"];
}
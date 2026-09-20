namespace Mississippi.Architecture.L0Tests.Fixtures.Dependencies;

/// <summary>Negative fixture proving helper return values retain constructor parameter provenance.</summary>
internal sealed class ReturnHelperAssignmentFixture
{
    private readonly IClockFixture clock;

    /// <summary>Initializes a new instance of the <see cref="ReturnHelperAssignmentFixture" /> class.</summary>
    /// <param name="clock">The dependency normalized by the helper.</param>
    public ReturnHelperAssignmentFixture(
        IClockFixture clock
    ) =>
        this.clock = Normalize(clock);

    private static IClockFixture Normalize(
        IClockFixture value
    ) =>
        value;

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependency.</returns>
    public IClockFixture GetClock() => clock;
}
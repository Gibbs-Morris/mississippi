namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Negative fixture proving dependency provenance follows same-type assignment helpers.</summary>
internal sealed class HelperAssignmentFixture
{
    private IClockFixture clock = null!;

    /// <summary>Initializes a new instance of the <see cref="HelperAssignmentFixture" /> class.</summary>
    /// <param name="clock">The dependency stored by the helper.</param>
    public HelperAssignmentFixture(
        IClockFixture clock
    ) =>
        AssignClock(clock);

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependency.</returns>
    public IClockFixture GetClock() => clock;

#pragma warning disable S2325 // The instance receiver is intentional: the rule must follow helper storage on this type.
    private void AssignClock(
        IClockFixture value
    ) =>
        clock = value;
#pragma warning restore S2325
}
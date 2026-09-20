using System;


namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Negative fixture proving null-guarded constructor storage preserves parameter provenance.</summary>
internal sealed class NullGuardAssignmentFixture
{
    private readonly IClockFixture clock = null!;

    /// <summary>Initializes a new instance of the <see cref="NullGuardAssignmentFixture" /> class.</summary>
    /// <param name="clock">The dependency stored after null validation.</param>
    public NullGuardAssignmentFixture(
        IClockFixture clock
    ) =>
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependency.</returns>
    public IClockFixture GetClock() => clock;
}
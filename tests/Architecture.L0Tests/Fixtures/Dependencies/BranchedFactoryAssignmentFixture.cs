using System;


namespace Mississippi.Architecture.L0Tests.Fixtures.Dependencies;

/// <summary>Positive fixture proving validation branches do not taint later factory assignments.</summary>
internal sealed class BranchedFactoryAssignmentFixture
{
    private readonly IClockFixture clock;

    /// <summary>Initializes a new instance of the <see cref="BranchedFactoryAssignmentFixture" /> class.</summary>
    /// <param name="candidate">Candidate dependency used for validation only.</param>
    public BranchedFactoryAssignmentFixture(
        IClockFixture candidate
    )
    {
        ArgumentNullException.ThrowIfNull(candidate);
        clock = GetDefault();
    }

    private static IClockFixture GetDefault() => null!;

    /// <summary>Reads the factory-assigned value.</summary>
    /// <returns>The default dependency.</returns>
    public IClockFixture GetClock() => clock;
}
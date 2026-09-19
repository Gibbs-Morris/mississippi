namespace Mississippi.Architecture.L0Tests;

/// <summary>Positive fixture whose interface field receives unrelated factory state.</summary>
internal sealed class FactoryAssignmentFixture
{
    private readonly IClockFixture clock;

    /// <summary>Initializes a new instance of the <see cref="FactoryAssignmentFixture"/> class.</summary>
    /// <param name="candidate">Candidate dependency used for validation only.</param>
    public FactoryAssignmentFixture(IClockFixture candidate)
    {
        Validate(candidate);
        clock = GetDefault();
    }

    /// <summary>Returns the factory-assigned value.</summary>
    /// <returns>The default dependency.</returns>
    public IClockFixture GetClock() => clock;

    private static void Validate(IClockFixture candidate)
    {
        _ = candidate;
    }

    private static IClockFixture GetDefault() => null!;
}

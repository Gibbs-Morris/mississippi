namespace Mississippi.Architecture.L0Tests;

/// <summary>Negative fixture for compiler-generated primary-constructor capture storage.</summary>
internal sealed class PrimaryConstructorFixture(IClockFixture clock)
{
    /// <summary>Reads the captured dependency.</summary>
    /// <returns>The captured dependency.</returns>
    public IClockFixture GetClock() => clock;
}

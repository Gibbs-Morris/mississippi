namespace Mississippi.Architecture.L0Tests.Fixtures.Dependencies;

/// <summary>Negative fixture proving a widened storage type still retains dependency provenance.</summary>
internal sealed class ObjectStorageFixture
{
    private readonly object clock;

    /// <summary>Initializes a new instance of the <see cref="ObjectStorageFixture" /> class.</summary>
    /// <param name="clock">The dependency stored behind an object type.</param>
    public ObjectStorageFixture(
        IClockFixture clock
    ) =>
        this.clock = clock;

    /// <summary>Reads the stored value so the fixture remains executable.</summary>
    /// <returns>The stored value.</returns>
    public object GetClock() => clock;
}
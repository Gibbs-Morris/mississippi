namespace Mississippi.Architecture.L0Tests.Fixtures.Dependencies;

/// <summary>Negative fixture proving helper provenance maps the correct argument in multi-argument calls.</summary>
internal sealed class MultiArgumentHelperFixture
{
    private IClockFixture clock = null!;

    /// <summary>Initializes a new instance of the <see cref="MultiArgumentHelperFixture" /> class.</summary>
    /// <param name="options">An unrelated helper argument.</param>
    /// <param name="clock">The dependency stored by the helper.</param>
    public MultiArgumentHelperFixture(
        IClockFixture options,
        IClockFixture clock
    ) =>
        Assign(options, clock);

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependency.</returns>
    public IClockFixture GetClock() => clock;

    private void Assign(
        IClockFixture options,
        IClockFixture value
    )
    {
        _ = options;
        clock = value;
    }
}
namespace Mississippi.Architecture.L0Tests.Fixtures.Dependencies;

/// <summary>Negative fixture proving inherited manual property setters are included.</summary>
internal sealed class InheritedManualPropertyFixture : InheritedManualPropertyBase
{
    /// <summary>Initializes a new instance of the <see cref="InheritedManualPropertyFixture" /> class.</summary>
    /// <param name="clock">The dependency assigned through the inherited setter.</param>
    public InheritedManualPropertyFixture(
        IClockFixture clock
    ) =>
        Clock = clock;
}
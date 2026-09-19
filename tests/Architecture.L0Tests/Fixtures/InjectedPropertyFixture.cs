namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Positive fixture that stores an injected dependency in a get-only property.</summary>
internal sealed class InjectedPropertyFixture
{
    /// <summary>Initializes a new instance of the <see cref="InjectedPropertyFixture"/> class.</summary>
    /// <param name="clock">Dependency stored by the positive fixture.</param>
    public InjectedPropertyFixture(IClockFixture clock) => Clock = clock;

    /// <summary>Gets the injected dependency.</summary>
    internal IClockFixture Clock { get; }
}

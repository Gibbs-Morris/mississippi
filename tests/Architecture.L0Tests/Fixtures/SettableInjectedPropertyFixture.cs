namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>
///     Negative fixture that stores an injected dependency in a settable property.
/// </summary>
internal sealed class SettableInjectedPropertyFixture
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SettableInjectedPropertyFixture"/> class.
    /// </summary>
    /// <param name="clock">Dependency stored by the negative fixture.</param>
    public SettableInjectedPropertyFixture(IClockFixture clock) => Clock = clock;

    /// <summary>
    ///     Gets or sets the injected dependency.
    /// </summary>
    internal IClockFixture Clock { get; set; }
}

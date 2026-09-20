namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Negative fixture that stores an injected dependency in an inherited field.</summary>
internal sealed class InheritedInjectedFieldFixture : InheritedInjectedFieldBase
{
    /// <summary>Initializes a new instance of the <see cref="InheritedInjectedFieldFixture"/> class.</summary>
    /// <param name="clock">Dependency stored in the inherited field.</param>
    public InheritedInjectedFieldFixture(IClockFixture clock) => this.clock = clock;
}

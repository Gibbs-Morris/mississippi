using System;

namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Negative fixture that stores a dependency behind nested generic and array wrappers.</summary>
internal sealed class NestedWrappedInjectedFieldFixture
{
    private readonly Lazy<IClockFixture[]> clocks;

    /// <summary>Initializes a new instance of the <see cref="NestedWrappedInjectedFieldFixture"/> class.</summary>
    /// <param name="clocks">Nested wrapped dependency.</param>
    public NestedWrappedInjectedFieldFixture(Lazy<IClockFixture[]> clocks) => this.clocks = clocks;

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependencies.</returns>
    public IClockFixture[] GetClock() => clocks.Value;
}

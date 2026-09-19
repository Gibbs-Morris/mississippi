using System;

namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>
///     Controlled fixture for wrapped interface dependency detection.
/// </summary>
internal sealed class WrappedInjectedFieldFixture
{
    /// <summary>
    ///     Stores a wrapped dependency in a field.
    /// </summary>
    private readonly Lazy<IClockFixture> clock;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WrappedInjectedFieldFixture"/> class.
    /// </summary>
    /// <param name="clock">Wrapped clock dependency.</param>
    public WrappedInjectedFieldFixture(Lazy<IClockFixture> clock)
    {
        this.clock = clock;
    }

    /// <summary>
    ///     Gets the wrapped clock.
    /// </summary>
    /// <returns>The clock dependency.</returns>
    public IClockFixture GetClock() => clock.Value;
}

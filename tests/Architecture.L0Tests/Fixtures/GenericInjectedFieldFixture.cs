namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Negative fixture proving generic parameter constraints preserve dependency detection.</summary>
/// <typeparam name="TClock">Constrained clock dependency type.</typeparam>
internal sealed class GenericInjectedFieldFixture<TClock>
    where TClock : IClockFixture
{
    private readonly TClock clock;

    /// <summary>Initializes a new instance of the <see cref="GenericInjectedFieldFixture{TClock}" /> class.</summary>
    /// <param name="clock">Constrained dependency stored by the fixture.</param>
    public GenericInjectedFieldFixture(TClock clock) => this.clock = clock;

    /// <summary>Reads the dependency so the fixture remains executable.</summary>
    /// <returns>The captured dependency.</returns>
    public TClock GetClock() => clock;
}

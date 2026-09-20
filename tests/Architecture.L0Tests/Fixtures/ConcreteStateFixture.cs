namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Positive fixture proving explicitly classified service names do not include ordinary state.</summary>
internal sealed class ConcreteStateFixture
{
    private readonly ConcreteState state;

    /// <summary>Initializes a new instance of the <see cref="ConcreteStateFixture" /> class.</summary>
    /// <param name="state">Ordinary concrete state.</param>
    public ConcreteStateFixture(ConcreteState state) => this.state = state;

    /// <summary>Reads the state so the fixture remains executable.</summary>
    /// <returns>The state value.</returns>
    public int GetValue() => state.Value;
}

using System.Collections.Generic;


namespace Mississippi.Architecture.L0Tests;

/// <summary>Positive fixture representing ordinary concrete implementation state.</summary>
internal sealed class OrdinaryStateFixture
{
    private readonly List<string> values;

    /// <summary>Initializes a new instance of the <see cref="OrdinaryStateFixture"/> class.</summary>
    /// <param name="values">Concrete implementation state.</param>
    public OrdinaryStateFixture(List<string> values) => this.values = values;

    /// <summary>Gets the state count.</summary>
    public int Count => values.Count;
}

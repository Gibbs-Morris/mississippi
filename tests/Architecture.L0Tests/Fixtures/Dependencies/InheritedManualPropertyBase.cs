using System.Collections.Generic;


namespace Mississippi.Architecture.L0Tests.Fixtures.Dependencies;

/// <summary>Base fixture storing a manually implemented dependency property in a collection.</summary>
internal class InheritedManualPropertyBase
{
    private readonly Dictionary<string, object> values = new();

    /// <summary>Gets or sets the inherited dependency property.</summary>
    protected IClockFixture Clock
    {
        get => (IClockFixture)values[nameof(Clock)];
        set => values[nameof(Clock)] = value;
    }
}
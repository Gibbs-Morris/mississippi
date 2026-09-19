namespace Mississippi.Architecture.L0Tests;

/// <summary>Readonly struct used by the readonly advisory fixture.</summary>
internal readonly struct ReadonlyFixture
{
    /// <summary>Initializes a new instance of the <see cref="ReadonlyFixture"/> struct.</summary>
    /// <param name="value">Initial value.</param>
    public ReadonlyFixture(int value) => Value = value;

    /// <summary>Gets the fixture value.</summary>
    public int Value { get; }
}

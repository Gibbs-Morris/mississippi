namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Ordinary model/state type that is not classified as a concrete service.</summary>
internal sealed class ConcreteState
{
    /// <summary>Initializes a new instance of the <see cref="ConcreteState" /> class.</summary>
    public ConcreteState() => Value = 1;

    /// <summary>Gets a deterministic fixture value.</summary>
    public int Value { get; }
}
namespace Mississippi.Architecture.L0Tests.Fixtures.Dependencies;

/// <summary>Concrete dependency with no abstraction, matching a registered concrete service.</summary>
internal sealed class ConcreteInjectedDependency
{
    /// <summary>Initializes a new instance of the <see cref="ConcreteInjectedDependency" /> class.</summary>
    public ConcreteInjectedDependency() => Value = 1;

    /// <summary>Gets a deterministic fixture value.</summary>
    public int Value { get; }
}
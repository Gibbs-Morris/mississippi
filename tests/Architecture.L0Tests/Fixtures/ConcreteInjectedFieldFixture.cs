using Mississippi.Architecture.L0Tests.Fixtures.Dependencies;


namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Concrete collaborator used to prove concrete service-shaped fields are detected.</summary>
internal sealed class ConcreteInjectedFieldFixture
{
    private readonly ConcreteInjectedDependency dependency;

    /// <summary>Initializes a new instance of the <see cref="ConcreteInjectedFieldFixture" /> class.</summary>
    /// <param name="dependency">The concrete collaborator stored by the fixture.</param>
    public ConcreteInjectedFieldFixture(
        ConcreteInjectedDependency dependency
    ) =>
        this.dependency = dependency;

    /// <summary>Reads the collaborator so the fixture remains executable.</summary>
    /// <returns>The dependency value.</returns>
    public int GetValue() => dependency.Value;
}
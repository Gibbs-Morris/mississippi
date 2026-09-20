using System.Collections.Generic;


namespace Mississippi.Architecture.L0Tests.Fixtures;

/// <summary>Negative fixture proving concrete service-shaped elements are detected in collections.</summary>
internal sealed class ConcreteCollectionInjectedFieldFixture
{
    private readonly IReadOnlyList<ConcreteInjectedDependency> dependencies;

    /// <summary>Initializes a new instance of the <see cref="ConcreteCollectionInjectedFieldFixture" /> class.</summary>
    /// <param name="dependencies">Concrete collaborators stored by the fixture.</param>
    public ConcreteCollectionInjectedFieldFixture(
        IReadOnlyList<ConcreteInjectedDependency> dependencies
    ) =>
        this.dependencies = dependencies;

    /// <summary>Gets the collaborator count so the fixture remains executable.</summary>
    /// <returns>The captured collaborator count.</returns>
    public int Count => dependencies.Count;
}
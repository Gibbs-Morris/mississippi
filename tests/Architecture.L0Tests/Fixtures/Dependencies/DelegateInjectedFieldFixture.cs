using System;


namespace Mississippi.Architecture.L0Tests.Fixtures.Dependencies;

/// <summary>Negative fixture proving injected delegates are treated as collaborators.</summary>
internal sealed class DelegateInjectedFieldFixture
{
    private readonly Func<int> factory;

    /// <summary>Initializes a new instance of the <see cref="DelegateInjectedFieldFixture" /> class.</summary>
    /// <param name="factory">The injected behavior.</param>
    public DelegateInjectedFieldFixture(
        Func<int> factory
    ) =>
        this.factory = factory;

    /// <summary>Invokes the injected behavior.</summary>
    /// <returns>The delegate result.</returns>
    public int GetValue() => factory();
}
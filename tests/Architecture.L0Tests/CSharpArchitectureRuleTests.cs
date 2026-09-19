using System.Collections.Generic;

using Xunit;

#pragma warning disable CS1591, CS0649, SA1600, SA1502, S1144


namespace Mississippi.Architecture.L0Tests;

public sealed class CSharpArchitectureRuleTests
{
    private interface IClock { }

    private sealed class InjectedFieldFixture
    {
        private readonly IClock clock;

        public InjectedFieldFixture(IClock clock) => this.clock = clock;
    }

    private sealed class InjectedPropertyFixture
    {
        public InjectedPropertyFixture(IClock clock) => Clock = clock;

        private IClock Clock { get; }
    }

    private sealed class OrdinaryStateFixture
    {
        private readonly List<string> values;

        public OrdinaryStateFixture(List<string> values) => this.values = values;
    }

    private struct MutableFixture
    {
        public int Value;
    }

    private readonly struct ReadonlyFixture
    {
        public ReadonlyFixture(int value) => Value = value;

        public int Value { get; }
    }

    [Fact]
    public void ConstructorInjectedFieldFixtureIsRejectedButPropertyAndStateAreAccepted()
    {
        IReadOnlyList<string> violations = CSharpArchitectureTests.FindConstructorInjectedFields(
            [typeof(InjectedFieldFixture), typeof(InjectedPropertyFixture), typeof(OrdinaryStateFixture)]);

        Assert.Contains(violations, value => value.EndsWith("InjectedFieldFixture.clock", System.StringComparison.Ordinal));
        Assert.DoesNotContain(violations, value => value.Contains("InjectedPropertyFixture", System.StringComparison.Ordinal));
        Assert.DoesNotContain(violations, value => value.Contains("OrdinaryStateFixture", System.StringComparison.Ordinal));
    }

    [Fact]
    public void ReadonlyStructDiagnosticDistinguishesMutableAndReadonlyFixtures()
    {
        IReadOnlyList<string> advisory = CSharpArchitectureTests.FindNonReadonlyStructs(
            [typeof(MutableFixture), typeof(ReadonlyFixture)]);

        Assert.Contains(advisory, value => value.EndsWith("MutableFixture", System.StringComparison.Ordinal));
        Assert.DoesNotContain(advisory, value => value.EndsWith("ReadonlyFixture", System.StringComparison.Ordinal));
    }
}

#pragma warning restore CS1591, CS0649, SA1600, SA1502, S1144

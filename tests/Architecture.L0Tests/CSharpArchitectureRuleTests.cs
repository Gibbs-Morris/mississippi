using System;
using System.Collections.Generic;

using Mississippi.Architecture.L0Tests.Fixtures;

using Xunit;

namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Controlled negative and positive fixtures for the C# architecture diagnostics.
/// </summary>
public sealed class CSharpArchitectureRuleTests
{
    /// <summary>
    ///     Proves field storage is rejected while property storage and ordinary state are accepted.
    /// </summary>
    [Fact]
    public void ConstructorInjectedFieldFixtureIsRejectedButPropertyAndStateAreAccepted()
    {
        IClockFixture clock = null!;
        _ = new InjectedFieldFixture(clock).GetClock();
        _ = new InjectedPropertyFixture(clock).GetClock();
        _ = new SettableInjectedPropertyFixture(clock).Clock;
        _ = new ManualSettableInjectedPropertyFixture(clock).GetClock();
        _ = new OrdinaryStateFixture(new List<string>()).Count;
        _ = new PrimaryConstructorFixture(clock).GetClock();
        _ = new FactoryAssignmentFixture(clock).GetClock();
        _ = new WrappedInjectedFieldFixture(new Lazy<IClockFixture>(() => clock)).GetClock();
        _ = new NestedWrappedInjectedFieldFixture(new Lazy<IClockFixture[]>(() => [clock])).GetClock();
        _ = new InheritedInjectedFieldFixture(clock).GetClock();
        IReadOnlyList<string> violations = CSharpArchitectureTests.FindConstructorInjectedFields(
            [typeof(InjectedFieldFixture), typeof(InjectedPropertyFixture), typeof(SettableInjectedPropertyFixture), typeof(ManualSettableInjectedPropertyFixture), typeof(OrdinaryStateFixture), typeof(PrimaryConstructorFixture), typeof(FactoryAssignmentFixture), typeof(WrappedInjectedFieldFixture), typeof(NestedWrappedInjectedFieldFixture), typeof(InheritedInjectedFieldFixture), typeof(InheritedInjectedFieldBase), typeof(ConcreteInjectedFieldFixture)]);

        Assert.Contains(violations, value => value.StartsWith(typeof(InjectedFieldFixture).FullName + ".", System.StringComparison.Ordinal) && value.EndsWith(".clock", System.StringComparison.Ordinal));
        Assert.DoesNotContain(violations, value => value.StartsWith(typeof(InjectedPropertyFixture).FullName + ".", System.StringComparison.Ordinal));
        Assert.Contains(violations, value => value.StartsWith(typeof(SettableInjectedPropertyFixture).FullName + ".", System.StringComparison.Ordinal));
        Assert.Contains(violations, value => value.StartsWith(typeof(ManualSettableInjectedPropertyFixture).FullName + ".", System.StringComparison.Ordinal));
        Assert.DoesNotContain(violations, value => value.Contains("OrdinaryStateFixture", System.StringComparison.Ordinal));
        Assert.Contains(violations, value => value.Contains("PrimaryConstructorFixture", System.StringComparison.Ordinal));
        Assert.DoesNotContain(violations, value => value.Contains("FactoryAssignmentFixture", System.StringComparison.Ordinal));
        Assert.Contains(violations, value => value.StartsWith(typeof(WrappedInjectedFieldFixture).FullName + ".", System.StringComparison.Ordinal));
        Assert.Contains(violations, value => value.StartsWith(typeof(NestedWrappedInjectedFieldFixture).FullName + ".", System.StringComparison.Ordinal));
        Assert.Contains(violations, value => value.StartsWith(typeof(InheritedInjectedFieldFixture).FullName + ".", System.StringComparison.Ordinal));
        Assert.Contains(violations, value => value.StartsWith(typeof(ConcreteInjectedFieldFixture).FullName + ".", System.StringComparison.Ordinal));
    }

    /// <summary>
    ///     Proves the readonly advisory distinguishes mutable and readonly fixtures.
    /// </summary>
    [Fact]
    public void ReadonlyStructDiagnosticDistinguishesMutableAndReadonlyFixtures()
    {
        MutableFixture mutable = new() { Value = 1 };
        ReadonlyFixture immutable = new(1);
        _ = mutable.Value + immutable.Value;
        IReadOnlyList<string> advisory = CSharpArchitectureTests.FindNonReadonlyStructs(
            [typeof(MutableFixture), typeof(ReadonlyFixture)]);

        Assert.Contains(advisory, value => value.EndsWith("MutableFixture", System.StringComparison.Ordinal));
        Assert.DoesNotContain(advisory, value => value.EndsWith("ReadonlyFixture", System.StringComparison.Ordinal));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using Mississippi.Architecture.L0Tests.Fixtures;


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
        _ = new ManualFieldBackedInjectedPropertyFixture(clock).GetClock();
        _ = new OrdinaryStateFixture(new List<string>()).Count;
        _ = new ConcreteStateFixture(new()).GetValue();
        _ = new PrimaryConstructorFixture(clock).GetClock();
        _ = new GenericInjectedFieldFixture<IClockFixture>(clock).GetClock();
        _ = new FactoryAssignmentFixture(clock).GetClock();
        _ = new WrappedInjectedFieldFixture(new(() => clock)).GetClock();
        _ = new NestedWrappedInjectedFieldFixture(new(() => [clock])).GetClock();
        _ = new InheritedInjectedFieldFixture(clock).GetClock();
        IReadOnlyList<string> violations = CSharpArchitectureTests.FindConstructorInjectedFields(
        [
            typeof(InjectedFieldFixture), typeof(InjectedPropertyFixture), typeof(SettableInjectedPropertyFixture),
            typeof(ManualSettableInjectedPropertyFixture), typeof(ManualFieldBackedInjectedPropertyFixture),
            typeof(OrdinaryStateFixture), typeof(ConcreteStateFixture), typeof(PrimaryConstructorFixture),
            typeof(GenericInjectedFieldFixture<IClockFixture>), typeof(FactoryAssignmentFixture),
            typeof(WrappedInjectedFieldFixture), typeof(NestedWrappedInjectedFieldFixture),
            typeof(InheritedInjectedFieldFixture), typeof(InheritedInjectedFieldBase),
            typeof(ConcreteInjectedFieldFixture),
        ]);
        Assert.Contains($"{typeof(InjectedFieldFixture).FullName}.clock", violations);
        Assert.DoesNotContain($"{typeof(InjectedPropertyFixture).FullName}.Clock", violations);
        Assert.Contains($"{typeof(SettableInjectedPropertyFixture).FullName}.<Clock>k__BackingField", violations);
        Assert.Contains($"{typeof(ManualSettableInjectedPropertyFixture).FullName}.Clock", violations);
        Assert.Equal(
            1,
            violations.Count(value => value.StartsWith(
                typeof(ManualFieldBackedInjectedPropertyFixture).FullName + ".",
                StringComparison.Ordinal)));
        Assert.Contains($"{typeof(ManualFieldBackedInjectedPropertyFixture).FullName}.clock", violations);
        Assert.DoesNotContain(violations, value => value.Contains("OrdinaryStateFixture", StringComparison.Ordinal));
        Assert.DoesNotContain(
            violations,
            value => value.StartsWith(typeof(ConcreteStateFixture).FullName + ".", StringComparison.Ordinal));
        Assert.Contains($"{typeof(PrimaryConstructorFixture).FullName}.<clock>P", violations);
        Assert.Contains($"{typeof(GenericInjectedFieldFixture<IClockFixture>).FullName}.clock", violations);
        Assert.DoesNotContain(
            violations,
            value => value.StartsWith(typeof(FactoryAssignmentFixture).FullName + ".", StringComparison.Ordinal));
        Assert.Contains($"{typeof(WrappedInjectedFieldFixture).FullName}.clock", violations);
        Assert.Contains($"{typeof(NestedWrappedInjectedFieldFixture).FullName}.clocks", violations);
        Assert.Contains($"{typeof(InheritedInjectedFieldFixture).FullName}.<Clock>k__BackingField", violations);
        Assert.Contains($"{typeof(ConcreteInjectedFieldFixture).FullName}.dependency", violations);
    }

    /// <summary>
    ///     Proves the readonly advisory distinguishes mutable and readonly fixtures.
    /// </summary>
    [Fact]
    public void ReadonlyStructDiagnosticDistinguishesMutableAndReadonlyFixtures()
    {
        MutableFixture mutable = new()
        {
            Value = 1,
        };
        ReadonlyFixture immutable = new(1);
        _ = mutable.Value + immutable.Value;
        IReadOnlyList<string> advisory = CSharpArchitectureTests.FindNonReadonlyStructs(
            [typeof(MutableFixture), typeof(ReadonlyFixture)]);
        Assert.Contains(advisory, value => value.EndsWith("MutableFixture", StringComparison.Ordinal));
        Assert.DoesNotContain(advisory, value => value.EndsWith("ReadonlyFixture", StringComparison.Ordinal));
    }
}
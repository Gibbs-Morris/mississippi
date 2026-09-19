using System;
using System.Collections.Generic;

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
        _ = new InjectedPropertyFixture(clock).Clock;
        _ = new OrdinaryStateFixture(new List<string>()).Count;
        _ = new PrimaryConstructorFixture(clock).GetClock();
        _ = new FactoryAssignmentFixture(clock).GetClock();
        _ = new WrappedInjectedFieldFixture(new Lazy<IClockFixture>(() => clock)).GetClock();
        IReadOnlyList<string> violations = CSharpArchitectureTests.FindConstructorInjectedFields(
            [typeof(InjectedFieldFixture), typeof(InjectedPropertyFixture), typeof(OrdinaryStateFixture), typeof(PrimaryConstructorFixture), typeof(FactoryAssignmentFixture), typeof(WrappedInjectedFieldFixture)]);

        Assert.Contains(violations, value => value.EndsWith("InjectedFieldFixture.clock", System.StringComparison.Ordinal));
        Assert.DoesNotContain(violations, value => value.Contains("InjectedPropertyFixture", System.StringComparison.Ordinal));
        Assert.DoesNotContain(violations, value => value.Contains("OrdinaryStateFixture", System.StringComparison.Ordinal));
        Assert.Contains(violations, value => value.Contains("PrimaryConstructorFixture", System.StringComparison.Ordinal));
        Assert.DoesNotContain(violations, value => value.Contains("FactoryAssignmentFixture", System.StringComparison.Ordinal));
        Assert.Contains(violations, value => value.Contains("WrappedInjectedFieldFixture", System.StringComparison.Ordinal));
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

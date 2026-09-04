using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.TestHarness.Effects;

using Xunit.Sdk;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Effects;

/// <summary>
///     Verifies dispatch assertions match command type, aggregate type, and destination.
/// </summary>
public sealed class EffectTestExtensionsTests
{
    /// <summary>
    ///     Missing dispatch collections should identify the caller parameter rather than a LINQ source.
    /// </summary>
    [Fact]
    public void DispatchAssertionsShouldIdentifyMissingCollection()
    {
        IReadOnlyList<(Type AggregateType, string EntityId, object Command)> commands = null!;
        Assert.Throws<ArgumentNullException>("commands", () => commands.ShouldHaveDispatched<string>());
        Assert.Throws<ArgumentNullException>("commands", () => commands.ShouldHaveDispatchedTo<string>());
    }

    /// <summary>
    ///     Dispatch assertion helpers should reject missing results and command collections.
    /// </summary>
    [Fact]
    public void DispatchAssertionsShouldRejectNullInputs()
    {
        EffectTestResult result = null!;
        IReadOnlyList<(Type AggregateType, string EntityId, object Command)> commands = null!;
        Assert.Throws<ArgumentNullException>(() => new EffectTestResult(null!));
        Assert.Throws<ArgumentNullException>(() => commands.ShouldHaveDispatched<string>());
        Assert.Throws<ArgumentNullException>(() => result.ShouldHaveDispatched<string>());
        Assert.Throws<ArgumentNullException>(() => commands.ShouldHaveDispatchedTo<string>());
        Assert.Throws<ArgumentNullException>(() => result.ShouldHaveDispatchedTo<string>());
        Assert.Throws<ArgumentNullException>(() => commands.ShouldHaveNoDispatches());
        Assert.Throws<ArgumentNullException>(() => result.ShouldHaveNoDispatches());
    }

    /// <summary>
    ///     Dispatch lookup should distinguish aggregate type and optional entity identity.
    /// </summary>
    [Fact]
    public void DispatchAssertionsShouldSelectTheRequestedTarget()
    {
        IReadOnlyList<(Type AggregateType, string EntityId, object Command)> commands =
        [
            (typeof(string), "other", new Uri("https://example.com")),
            (typeof(List<string>), "first", "first-command"),
            (typeof(List<string>), "second", "second-command"),
        ];
        EffectTestResult result = new(commands);
        Assert.Same(commands, result.DispatchedCommands);
        Assert.Equal(3, result.DispatchCount);
        Assert.True(result.HasDispatches);
        Assert.Equal("first-command", commands.ShouldHaveDispatched<string>());
        Assert.Equal("first-command", result.ShouldHaveDispatched<string>());
        Assert.Equal(commands[1], commands.ShouldHaveDispatchedTo<List<string>>());
        Assert.Equal(commands[2], result.ShouldHaveDispatchedTo<List<string>>("second"));
        Assert.Throws<XunitException>(() => commands.ShouldHaveDispatched<Version>());
        Assert.Throws<XunitException>(() => result.ShouldHaveDispatchedTo<List<string>>("missing"));
        Assert.Throws<XunitException>(() => result.ShouldHaveNoDispatches());
    }

    /// <summary>
    ///     Empty results should support fluent no-dispatch assertions.
    /// </summary>
    [Fact]
    public void EmptyResultsShouldHaveNoDispatches()
    {
        EffectTestResult result = new([]);
        Assert.Same(result, result.ShouldHaveNoDispatches());
        Assert.False(result.HasDispatches);
        Assert.Equal(0, result.DispatchCount);
        result.DispatchedCommands.ShouldHaveNoDispatches();
        Assert.Throws<XunitException>(() => result.ShouldHaveDispatched<string>());
    }
}
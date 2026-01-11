using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotTypeRegistry" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Snapshot Type Registry")]
public class SnapshotTypeRegistryTests
{
    /// <summary>
    ///     Another test state record for multiple registration tests.
    /// </summary>
    /// <param name="Value">A dummy value for testing.</param>
    private sealed record AnotherState(int Value = 0);

    /// <summary>
    ///     Test state record for registration tests.
    /// </summary>
    /// <param name="Value">A dummy value for testing.</param>
    private sealed record TestState(int Value = 0);

    /// <summary>
    ///     Register should not overwrite existing registration with same name.
    /// </summary>
    [Fact]
    public void RegisterDoesNotOverwriteExisting()
    {
        SnapshotTypeRegistry registry = new();
        registry.Register("TestState", typeof(TestState));
        registry.Register("TestState", typeof(AnotherState));
        Type? resolved = registry.ResolveType("TestState");
        Assert.Equal(typeof(TestState), resolved);
    }

    /// <summary>
    ///     Register should store the type when called with valid arguments.
    /// </summary>
    [Fact]
    public void RegisterStoresSnapshotType()
    {
        SnapshotTypeRegistry registry = new();
        registry.Register("TestState", typeof(TestState));
        Type? resolved = registry.ResolveType("TestState");
        Assert.Equal(typeof(TestState), resolved);
    }

    /// <summary>
    ///     Register should throw when snapshot name is empty.
    /// </summary>
    /// <param name="snapshotName">The snapshot name to test.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterThrowsWhenSnapshotNameIsEmptyOrWhitespace(
        string snapshotName
    )
    {
        SnapshotTypeRegistry registry = new();
        Assert.Throws<ArgumentException>(() => registry.Register(snapshotName, typeof(TestState)));
    }

    /// <summary>
    ///     Register should throw when snapshot name is null.
    /// </summary>
    [Fact]
    public void RegisterThrowsWhenSnapshotNameIsNull()
    {
        SnapshotTypeRegistry registry = new();
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!, typeof(TestState)));
    }

    /// <summary>
    ///     Register should throw when snapshot type is null.
    /// </summary>
    [Fact]
    public void RegisterThrowsWhenSnapshotTypeIsNull()
    {
        SnapshotTypeRegistry registry = new();
        Assert.Throws<ArgumentNullException>(() => registry.Register("TestState", null!));
    }

    /// <summary>
    ///     RegisteredTypes should expose all registered types.
    /// </summary>
    [Fact]
    public void RegisteredTypesExposesAllRegistrations()
    {
        SnapshotTypeRegistry registry = new();
        registry.Register("TestState", typeof(TestState));
        registry.Register("AnotherState", typeof(AnotherState));
        Assert.Equal(2, registry.RegisteredTypes.Count);
        Assert.True(registry.RegisteredTypes.ContainsKey("TestState"));
        Assert.True(registry.RegisteredTypes.ContainsKey("AnotherState"));
    }

    /// <summary>
    ///     Registry should support multiple snapshot types.
    /// </summary>
    [Fact]
    public void RegistrySupportsMultipleSnapshotTypes()
    {
        SnapshotTypeRegistry registry = new();
        registry.Register("TestState", typeof(TestState));
        registry.Register("AnotherState", typeof(AnotherState));
        Assert.Equal(typeof(TestState), registry.ResolveType("TestState"));
        Assert.Equal(typeof(AnotherState), registry.ResolveType("AnotherState"));
    }

    /// <summary>
    ///     ResolveName should return the name for a registered type.
    /// </summary>
    [Fact]
    public void ResolveNameReturnsNameForRegisteredType()
    {
        SnapshotTypeRegistry registry = new();
        registry.Register("TestState", typeof(TestState));
        string? name = registry.ResolveName(typeof(TestState));
        Assert.Equal("TestState", name);
    }

    /// <summary>
    ///     ResolveName should return null when type is not registered.
    /// </summary>
    [Fact]
    public void ResolveNameReturnsNullWhenNotRegistered()
    {
        SnapshotTypeRegistry registry = new();
        string? name = registry.ResolveName(typeof(TestState));
        Assert.Null(name);
    }

    /// <summary>
    ///     ResolveName should throw when snapshot type is null.
    /// </summary>
    [Fact]
    public void ResolveNameThrowsWhenSnapshotTypeIsNull()
    {
        SnapshotTypeRegistry registry = new();
        Assert.Throws<ArgumentNullException>(() => registry.ResolveName(null!));
    }

    /// <summary>
    ///     ResolveType should be case-sensitive.
    /// </summary>
    [Fact]
    public void ResolveTypeIsCaseSensitive()
    {
        SnapshotTypeRegistry registry = new();
        registry.Register("TestState", typeof(TestState));
        Type? resolved = registry.ResolveType("teststate");
        Assert.Null(resolved);
    }

    /// <summary>
    ///     ResolveType should return null when type is not registered.
    /// </summary>
    [Fact]
    public void ResolveTypeReturnsNullWhenNotRegistered()
    {
        SnapshotTypeRegistry registry = new();
        Type? resolved = registry.ResolveType("UnknownState");
        Assert.Null(resolved);
    }

    /// <summary>
    ///     ResolveType should throw when snapshot type name is empty.
    /// </summary>
    /// <param name="snapshotTypeName">The snapshot type name to test.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveTypeThrowsWhenSnapshotTypeNameIsEmptyOrWhitespace(
        string snapshotTypeName
    )
    {
        SnapshotTypeRegistry registry = new();
        Assert.Throws<ArgumentException>(() => registry.ResolveType(snapshotTypeName));
    }

    /// <summary>
    ///     ResolveType should throw when snapshot type name is null.
    /// </summary>
    [Fact]
    public void ResolveTypeThrowsWhenSnapshotTypeNameIsNull()
    {
        SnapshotTypeRegistry registry = new();
        Assert.Throws<ArgumentNullException>(() => registry.ResolveType(null!));
    }
}
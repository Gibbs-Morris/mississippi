using System;
using System.Reflection;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions.Attributes;

using Mississippi.Tributary.Abstractions.Attributes;

using Moq;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotTypeRegistry" />.
/// </summary>
public class SnapshotTypeRegistryTests
{
    /// <summary>
    ///     Another test state record for multiple registration tests.
    /// </summary>
    /// <param name="Value">A dummy value for testing.</param>
    [SnapshotStorageName("TEST", "REGISTRY", "ANOTHERSNAPSHOT")]
    private sealed record AnotherState(int Value = 0);

    /// <summary>
    ///     Snapshot type with an invalid retention attribute.
    /// </summary>
    [SnapshotRetention(0)]
    private sealed record InvalidRetentionState;

    /// <summary>
    ///     Test state record for registration tests.
    /// </summary>
    /// <param name="Value">A dummy value for testing.</param>
    [SnapshotStorageName("TEST", "REGISTRY", "FIRSTSNAPSHOT")]
    private sealed record TestState(int Value = 0);

    /// <summary>
    ///     Concurrent aliases for one CLR type leave exactly one matching name/type pair.
    /// </summary>
    [Fact]
    public void ConcurrentAliasesKeepOneBidirectionalMapping()
    {
        SnapshotTypeRegistry registry = new();
        Parallel.For(0, 32, index => registry.Register($"Snapshot{index}", typeof(TestState)));
        string registeredName = Assert.Single(registry.RegisteredTypes).Key;
        Assert.Equal(registeredName, registry.ResolveName(typeof(TestState)));
        Assert.Equal(typeof(TestState), registry.ResolveType(registeredName));
    }

    /// <summary>
    ///     Ignoring a duplicate CLR type leaves its rejected alias available for another type.
    /// </summary>
    [Fact]
    public void DuplicateTypeDoesNotReserveAnotherName()
    {
        SnapshotTypeRegistry registry = new();
        registry.Register("First", typeof(TestState));
        registry.Register("Second", typeof(TestState));
        Assert.Null(registry.ResolveType("Second"));
        registry.Register("Second", typeof(AnotherState));
        Assert.Equal("First", registry.ResolveName(typeof(TestState)));
        Assert.Equal("Second", registry.ResolveName(typeof(AnotherState)));
        Assert.Equal(typeof(AnotherState), registry.ResolveType("Second"));
        Assert.Equal(2, registry.RegisteredTypes.Count);
    }

    /// <summary>
    ///     Scans count newly inserted mappings and become idempotent after registration.
    /// </summary>
    [Fact]
    public void ScanAssemblyCountsOnlyNewMappings()
    {
        SnapshotTypeRegistry registry = new();
        Mock<Assembly> assembly = new();
        assembly.Setup(instance => instance.GetTypes()).Returns([typeof(TestState), typeof(AnotherState), typeof(string)]);
        registry.Register("TEST.REGISTRY.FIRSTSNAPSHOT.V1", typeof(TestState));
        Assert.Equal(1, registry.ScanAssembly(assembly.Object));
        Assert.Equal(0, registry.ScanAssembly(assembly.Object));
        Assert.Equal(2, registry.RegisteredTypes.Count);
        Assert.Equal("TEST.REGISTRY.ANOTHERSNAPSHOT.V1", registry.ResolveName(typeof(AnotherState)));
    }

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
    ///     Verifies that invalid retention metadata is surfaced during snapshot registration.
    /// </summary>
    [Fact]
    public void RegisterSurfacesInvalidRetentionAttribute()
    {
        SnapshotTypeRegistry registry = new();
        Assert.Throws<ArgumentOutOfRangeException>(() => registry.Register(
            "InvalidRetentionState",
            typeof(InvalidRetentionState)));
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
    ///     RegisteredTypes should return empty dictionary initially.
    /// </summary>
    [Fact]
    public void RegisteredTypesReturnsEmptyDictionaryInitially()
    {
        SnapshotTypeRegistry registry = new();
        Assert.Empty(registry.RegisteredTypes);
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

    /// <summary>
    ///     ScanAssembly should return zero when no attributed types exist.
    /// </summary>
    [Fact]
    public void ScanAssemblyReturnsZeroForAssemblyWithNoAttributedTypes()
    {
        SnapshotTypeRegistry registry = new();

        // Use mscorlib which has no SnapshotStorageNameAttribute types
        int count = registry.ScanAssembly(typeof(object).Assembly);
        Assert.Equal(0, count);
    }

    /// <summary>
    ///     ScanAssembly should throw when assembly is null.
    /// </summary>
    [Fact]
    public void ScanAssemblyThrowsWhenAssemblyIsNull()
    {
        SnapshotTypeRegistry registry = new();
        Assert.Throws<ArgumentNullException>(() => registry.ScanAssembly(null!));
    }
}

using System;



namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="EventTypeRegistry" />.
/// </summary>
public class EventTypeRegistryTests
{
    /// <summary>
    ///     Another test event record for multiple registration tests.
    /// </summary>
    private sealed record AnotherEvent;

    /// <summary>
    ///     Test event record for registration tests.
    /// </summary>
    private sealed record TestEvent;

    /// <summary>
    ///     Register should not overwrite existing registration with same name.
    /// </summary>
    [Fact]
    public void RegisterDoesNotOverwriteExisting()
    {
        EventTypeRegistry registry = new();
        registry.Register("TestEvent", typeof(TestEvent));
        registry.Register("TestEvent", typeof(AnotherEvent));
        Type? resolved = registry.ResolveType("TestEvent");
        Assert.Equal(typeof(TestEvent), resolved);
    }

    /// <summary>
    ///     Register should store the type when called with valid arguments.
    /// </summary>
    [Fact]
    public void RegisterStoresEventType()
    {
        EventTypeRegistry registry = new();
        registry.Register("TestEvent", typeof(TestEvent));
        Type? resolved = registry.ResolveType("TestEvent");
        Assert.Equal(typeof(TestEvent), resolved);
    }

    /// <summary>
    ///     Register should throw when event name is empty.
    /// </summary>
    /// <param name="eventName">The event name to test.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterThrowsWhenEventNameIsEmptyOrWhitespace(
        string eventName
    )
    {
        EventTypeRegistry registry = new();
        Assert.Throws<ArgumentException>(() => registry.Register(eventName, typeof(TestEvent)));
    }

    /// <summary>
    ///     Register should throw when event name is null.
    /// </summary>
    [Fact]
    public void RegisterThrowsWhenEventNameIsNull()
    {
        EventTypeRegistry registry = new();
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!, typeof(TestEvent)));
    }

    /// <summary>
    ///     Register should throw when event type is null.
    /// </summary>
    [Fact]
    public void RegisterThrowsWhenEventTypeIsNull()
    {
        EventTypeRegistry registry = new();
        Assert.Throws<ArgumentNullException>(() => registry.Register("TestEvent", null!));
    }

    /// <summary>
    ///     RegisteredTypes should contain all registered types.
    /// </summary>
    [Fact]
    public void RegisteredTypesContainsRegisteredTypes()
    {
        EventTypeRegistry registry = new();
        registry.Register("TestEvent", typeof(TestEvent));
        registry.Register("AnotherEvent", typeof(AnotherEvent));
        Assert.Equal(2, registry.RegisteredTypes.Count);
        Assert.True(registry.RegisteredTypes.ContainsKey("TestEvent"));
        Assert.True(registry.RegisteredTypes.ContainsKey("AnotherEvent"));
    }

    /// <summary>
    ///     RegisteredTypes should return empty dictionary initially.
    /// </summary>
    [Fact]
    public void RegisteredTypesReturnsEmptyDictionaryInitially()
    {
        EventTypeRegistry registry = new();
        Assert.Empty(registry.RegisteredTypes);
    }

    /// <summary>
    ///     Registry should support multiple event types.
    /// </summary>
    [Fact]
    public void RegistrySupportsMultipleEventTypes()
    {
        EventTypeRegistry registry = new();
        registry.Register("TestEvent", typeof(TestEvent));
        registry.Register("AnotherEvent", typeof(AnotherEvent));
        Assert.Equal(typeof(TestEvent), registry.ResolveType("TestEvent"));
        Assert.Equal(typeof(AnotherEvent), registry.ResolveType("AnotherEvent"));
    }

    /// <summary>
    ///     ResolveName should return the registered name for a type.
    /// </summary>
    [Fact]
    public void ResolveNameReturnsNameForRegisteredType()
    {
        EventTypeRegistry registry = new();
        registry.Register("TestEvent", typeof(TestEvent));
        string? resolved = registry.ResolveName(typeof(TestEvent));
        Assert.Equal("TestEvent", resolved);
    }

    /// <summary>
    ///     ResolveName should return null for unregistered type.
    /// </summary>
    [Fact]
    public void ResolveNameReturnsNullForUnregisteredType()
    {
        EventTypeRegistry registry = new();
        string? resolved = registry.ResolveName(typeof(TestEvent));
        Assert.Null(resolved);
    }

    /// <summary>
    ///     ResolveName should throw when event type is null.
    /// </summary>
    [Fact]
    public void ResolveNameThrowsWhenEventTypeIsNull()
    {
        EventTypeRegistry registry = new();
        Assert.Throws<ArgumentNullException>(() => registry.ResolveName(null!));
    }

    /// <summary>
    ///     ResolveType should be case-sensitive.
    /// </summary>
    [Fact]
    public void ResolveTypeIsCaseSensitive()
    {
        EventTypeRegistry registry = new();
        registry.Register("TestEvent", typeof(TestEvent));
        Type? resolved = registry.ResolveType("testevent");
        Assert.Null(resolved);
    }

    /// <summary>
    ///     ResolveType should return null when type is not registered.
    /// </summary>
    [Fact]
    public void ResolveTypeReturnsNullWhenNotRegistered()
    {
        EventTypeRegistry registry = new();
        Type? resolved = registry.ResolveType("UnknownEvent");
        Assert.Null(resolved);
    }

    /// <summary>
    ///     ResolveType should throw when event type name is empty.
    /// </summary>
    /// <param name="eventTypeName">The event type name to test.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveTypeThrowsWhenEventTypeNameIsEmptyOrWhitespace(
        string eventTypeName
    )
    {
        EventTypeRegistry registry = new();
        Assert.Throws<ArgumentException>(() => registry.ResolveType(eventTypeName));
    }

    /// <summary>
    ///     ResolveType should throw when event type name is null.
    /// </summary>
    [Fact]
    public void ResolveTypeThrowsWhenEventTypeNameIsNull()
    {
        EventTypeRegistry registry = new();
        Assert.Throws<ArgumentNullException>(() => registry.ResolveType(null!));
    }

    /// <summary>
    ///     ScanAssembly should return zero when no attributed types exist.
    /// </summary>
    [Fact]
    public void ScanAssemblyReturnsZeroForAssemblyWithNoAttributedTypes()
    {
        EventTypeRegistry registry = new();

        // Use mscorlib which has no EventStorageNameAttribute types
        int count = registry.ScanAssembly(typeof(object).Assembly);
        Assert.Equal(0, count);
    }

    /// <summary>
    ///     ScanAssembly should throw when assembly is null.
    /// </summary>
    [Fact]
    public void ScanAssemblyThrowsWhenAssemblyIsNull()
    {
        EventTypeRegistry registry = new();
        Assert.Throws<ArgumentNullException>(() => registry.ScanAssembly(null!));
    }
}
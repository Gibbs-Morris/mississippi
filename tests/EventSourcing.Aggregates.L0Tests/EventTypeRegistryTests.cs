using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="EventTypeRegistry" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Event Type Registry")]
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
}
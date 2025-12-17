using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Abstractions.Attributes.Tests;

/// <summary>
///     Tests for <see cref="EventNameHelper" /> functionality.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Abstractions")]
[AllureSubSuite("Event Name Helper")]
public class EventNameHelperTests
{
    /// <summary>
    ///     Test fixture decorated with EventName attribute.
    /// </summary>
    [EventName("APP", "MODULE", "EVENT")]
    private sealed class DecoratedEvent
    {
    }

    /// <summary>
    ///     Test fixture without EventName attribute.
    /// </summary>
    private sealed class UndecoratedEvent
    {
    }

    /// <summary>
    ///     Test fixture with versioned EventName attribute.
    /// </summary>
    [EventName("APP", "MODULE", "EVENT", 2)]
    private sealed class VersionedEvent
    {
    }

    /// <summary>
    ///     GetEventName should return the event name from the attribute.
    /// </summary>
    [Fact]
    public void GetEventNameReturnsAttributeValue()
    {
        string eventName = EventNameHelper.GetEventName<DecoratedEvent>();
        Assert.Equal("APP.MODULE.EVENT.V1", eventName);
    }

    /// <summary>
    ///     GetEventName should return versioned event name.
    /// </summary>
    [Fact]
    public void GetEventNameReturnsVersionedName()
    {
        string eventName = EventNameHelper.GetEventName<VersionedEvent>();
        Assert.Equal("APP.MODULE.EVENT.V2", eventName);
    }

    /// <summary>
    ///     GetEventName should throw when type lacks the attribute.
    /// </summary>
    [Fact]
    public void GetEventNameThrowsWhenAttributeMissing()
    {
        Assert.Throws<InvalidOperationException>(() => EventNameHelper.GetEventName<UndecoratedEvent>());
    }

    /// <summary>
    ///     GetEventName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void GetEventNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => EventNameHelper.GetEventName(null!));
    }

    /// <summary>
    ///     TryGetEventName should return false when attribute is missing.
    /// </summary>
    [Fact]
    public void TryGetEventNameReturnsFalseWhenAttributeMissing()
    {
        bool result = EventNameHelper.TryGetEventName<UndecoratedEvent>(out string? eventName);
        Assert.False(result);
        Assert.Null(eventName);
    }

    /// <summary>
    ///     TryGetEventName should return true and the event name when attribute exists.
    /// </summary>
    [Fact]
    public void TryGetEventNameReturnsTrueWhenAttributeExists()
    {
        bool result = EventNameHelper.TryGetEventName<DecoratedEvent>(out string? eventName);
        Assert.True(result);
        Assert.Equal("APP.MODULE.EVENT.V1", eventName);
    }

    /// <summary>
    ///     TryGetEventName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void TryGetEventNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => EventNameHelper.TryGetEventName(null!, out string? _));
    }
}
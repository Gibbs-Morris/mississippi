using System;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions.Subscriptions;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests.Subscriptions;

/// <summary>
///     Tests for UX projection subscription types.
/// </summary>
public sealed class UxProjectionSubscriptionTypesTests
{
    /// <summary>
    ///     UxProjectionChangedEvent should create with all required properties.
    /// </summary>
    [Fact]
    public void UxProjectionChangedEventCreatesWithAllProperties()
    {
        UxProjectionKey projectionKey = new("user-123");
        BrookPosition newVersion = new(42);
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        UxProjectionChangedEvent changedEvent = new()
        {
            ProjectionKey = projectionKey,
            NewVersion = newVersion,
            Timestamp = timestamp,
        };
        Assert.Equal(projectionKey, changedEvent.ProjectionKey);
        Assert.Equal(newVersion, changedEvent.NewVersion);
        Assert.Equal(timestamp, changedEvent.Timestamp);
    }

    /// <summary>
    ///     UxProjectionChangedEvent should detect inequality when version differs.
    /// </summary>
    [Fact]
    public void UxProjectionChangedEventDetectsInequalityOnVersion()
    {
        UxProjectionKey projectionKey = new("user-123");
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        UxProjectionChangedEvent event1 = new()
        {
            ProjectionKey = projectionKey,
            NewVersion = new(42),
            Timestamp = timestamp,
        };
        UxProjectionChangedEvent event2 = new()
        {
            ProjectionKey = projectionKey,
            NewVersion = new(100),
            Timestamp = timestamp,
        };
        Assert.NotEqual(event1, event2);
        Assert.True(event1 != event2);
    }

    /// <summary>
    ///     UxProjectionChangedEvent should support equality comparison.
    /// </summary>
    [Fact]
    public void UxProjectionChangedEventSupportsEquality()
    {
        UxProjectionKey projectionKey = new("user-123");
        BrookPosition newVersion = new(42);
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        UxProjectionChangedEvent event1 = new()
        {
            ProjectionKey = projectionKey,
            NewVersion = newVersion,
            Timestamp = timestamp,
        };
        UxProjectionChangedEvent event2 = new()
        {
            ProjectionKey = projectionKey,
            NewVersion = newVersion,
            Timestamp = timestamp,
        };
        Assert.Equal(event1, event2);
        Assert.True(event1 == event2);
    }

    /// <summary>
    ///     UxProjectionSubscriptionRequest should create with all required properties.
    /// </summary>
    [Fact]
    public void UxProjectionSubscriptionRequestCreatesWithAllProperties()
    {
        UxProjectionSubscriptionRequest request = new()
        {
            ProjectionType = "UserProfile",
            BrookType = "UserEvents",
            EntityId = "user-123",
            ClientSubscriptionId = "sub-456",
        };
        Assert.Equal("UserProfile", request.ProjectionType);
        Assert.Equal("UserEvents", request.BrookType);
        Assert.Equal("user-123", request.EntityId);
        Assert.Equal("sub-456", request.ClientSubscriptionId);
    }

    /// <summary>
    ///     UxProjectionSubscriptionRequest should detect inequality when properties differ.
    /// </summary>
    [Fact]
    public void UxProjectionSubscriptionRequestDetectsInequality()
    {
        UxProjectionSubscriptionRequest request1 = new()
        {
            ProjectionType = "UserProfile",
            BrookType = "UserEvents",
            EntityId = "user-123",
            ClientSubscriptionId = "sub-456",
        };
        UxProjectionSubscriptionRequest request2 = new()
        {
            ProjectionType = "UserProfile",
            BrookType = "UserEvents",
            EntityId = "user-999",
            ClientSubscriptionId = "sub-456",
        };
        Assert.NotEqual(request1, request2);
        Assert.True(request1 != request2);
    }

    /// <summary>
    ///     UxProjectionSubscriptionRequest should support equality comparison.
    /// </summary>
    [Fact]
    public void UxProjectionSubscriptionRequestSupportsEquality()
    {
        UxProjectionSubscriptionRequest request1 = new()
        {
            ProjectionType = "UserProfile",
            BrookType = "UserEvents",
            EntityId = "user-123",
            ClientSubscriptionId = "sub-456",
        };
        UxProjectionSubscriptionRequest request2 = new()
        {
            ProjectionType = "UserProfile",
            BrookType = "UserEvents",
            EntityId = "user-123",
            ClientSubscriptionId = "sub-456",
        };
        Assert.Equal(request1, request2);
        Assert.True(request1 == request2);
    }
}
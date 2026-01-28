using System;
using System.Collections.Generic;


using Mississippi.Refraction.Events;


namespace Mississippi.Refraction.L0Tests.Events;

/// <summary>
///     Tests for <see cref="NavigationRequestedEvent" /> record.
/// </summary>
public sealed class NavigationRequestedEventTests
{
    /// <summary>
    ///     NavigationRequestedEvent can be created with parameters.
    /// </summary>
    [Fact]
        public void NavigationRequestedEventCanBeCreatedWithParameters()
    {
        // Arrange
        Dictionary<string, object> parameters = new()
        {
            ["id"] = 42,
        };

        // Act
        NavigationRequestedEvent evt = new("details", parameters);

        // Assert
        Assert.Equal("details", evt.Target);
        Assert.NotNull(evt.Parameters);
        Assert.Equal(42, evt.Parameters!["id"]);
    }

    /// <summary>
    ///     NavigationRequestedEvent can be created with target only.
    /// </summary>
    [Fact]
        public void NavigationRequestedEventCanBeCreatedWithTargetOnly()
    {
        // Arrange & Act
        NavigationRequestedEvent evt = new("/home");

        // Assert
        Assert.Equal("/home", evt.Target);
        Assert.Null(evt.Parameters);
    }

    /// <summary>
    ///     NavigationRequestedEvent implements record equality.
    /// </summary>
    [Fact]
        public void NavigationRequestedEventImplementsRecordEquality()
    {
        // Arrange
        NavigationRequestedEvent evt1 = new("/settings");
        NavigationRequestedEvent evt2 = new("/settings");

        // Assert
        Assert.Equal(evt1, evt2);
    }

    /// <summary>
    ///     NavigationRequestedEvent is sealed record.
    /// </summary>
    [Fact]
        public void NavigationRequestedEventIsSealedRecord()
    {
        // Arrange
        Type eventType = typeof(NavigationRequestedEvent);

        // Assert
        Assert.True(eventType.IsSealed);
        Assert.True(eventType.IsClass);
    }

    /// <summary>
    ///     NavigationRequestedEvent parameters defaults to null.
    /// </summary>
    [Fact]
        public void NavigationRequestedEventParametersDefaultsToNull()
    {
        // Arrange & Act
        NavigationRequestedEvent evt = new("/dashboard");

        // Assert
        Assert.Null(evt.Parameters);
    }
}
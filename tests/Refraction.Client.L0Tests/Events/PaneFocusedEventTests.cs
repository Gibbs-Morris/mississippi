using System;

using Mississippi.Refraction.Events;


namespace Mississippi.Refraction.L0Tests.Events;

/// <summary>
///     Tests for <see cref="PaneFocusedEvent" /> record.
/// </summary>
public sealed class PaneFocusedEventTests
{
    /// <summary>
    ///     PaneFocusedEvent can be created with pane id.
    /// </summary>
    [Fact]
    public void PaneFocusedEventCanBeCreatedWithPaneId()
    {
        // Arrange & Act
        PaneFocusedEvent evt = new("main-pane");

        // Assert
        Assert.Equal("main-pane", evt.PaneId);
    }

    /// <summary>
    ///     PaneFocusedEvent implements record equality.
    /// </summary>
    [Fact]
    public void PaneFocusedEventImplementsRecordEquality()
    {
        // Arrange
        PaneFocusedEvent evt1 = new("sidebar");
        PaneFocusedEvent evt2 = new("sidebar");

        // Assert
        Assert.Equal(evt1, evt2);
    }

    /// <summary>
    ///     PaneFocusedEvent is sealed record.
    /// </summary>
    [Fact]
    public void PaneFocusedEventIsSealedRecord()
    {
        // Arrange
        Type eventType = typeof(PaneFocusedEvent);

        // Assert
        Assert.True(eventType.IsSealed);
        Assert.True(eventType.IsClass);
    }

    /// <summary>
    ///     PaneFocusedEvent PaneId property is accessible.
    /// </summary>
    [Fact]
    public void PaneFocusedEventPaneIdPropertyIsAccessible()
    {
        // Arrange
        PaneFocusedEvent evt = new("detail-pane");

        // Act
        string paneId = evt.PaneId;

        // Assert
        Assert.Equal("detail-pane", paneId);
    }
}
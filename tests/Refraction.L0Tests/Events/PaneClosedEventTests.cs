using System;

using Allure.Xunit.Attributes;

using Mississippi.Refraction.Events;


namespace Mississippi.Refraction.L0Tests.Events;

/// <summary>
///     Tests for <see cref="PaneClosedEvent" /> record.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Events")]
public sealed class PaneClosedEventTests
{
    /// <summary>
    ///     PaneClosedEvent can be created with reason.
    /// </summary>
    [Fact]
    [AllureFeature("PaneClosedEvent")]
    public void PaneClosedEventCanBeCreatedWithReason()
    {
        // Arrange & Act
        PaneClosedEvent evt = new("keyboard-escape");

        // Assert
        Assert.Equal("keyboard-escape", evt.Reason);
    }

    /// <summary>
    ///     PaneClosedEvent implements record equality.
    /// </summary>
    [Fact]
    [AllureFeature("PaneClosedEvent")]
    public void PaneClosedEventImplementsRecordEquality()
    {
        // Arrange
        PaneClosedEvent evt1 = new("click-outside");
        PaneClosedEvent evt2 = new("click-outside");

        // Assert
        Assert.Equal(evt1, evt2);
    }

    /// <summary>
    ///     PaneClosedEvent is sealed record.
    /// </summary>
    [Fact]
    [AllureFeature("PaneClosedEvent")]
    public void PaneClosedEventIsSealedRecord()
    {
        // Arrange
        Type eventType = typeof(PaneClosedEvent);

        // Assert
        Assert.True(eventType.IsSealed);
        Assert.True(eventType.IsClass);
    }

    /// <summary>
    ///     PaneClosedEvent Reason property is accessible.
    /// </summary>
    [Fact]
    [AllureFeature("PaneClosedEvent")]
    public void PaneClosedEventReasonPropertyIsAccessible()
    {
        // Arrange
        PaneClosedEvent evt = new("action-complete");

        // Act
        string reason = evt.Reason;

        // Assert
        Assert.Equal("action-complete", reason);
    }
}
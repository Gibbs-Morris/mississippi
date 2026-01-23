using System;

using Allure.Xunit.Attributes;

using Mississippi.Refraction.Events;


namespace Mississippi.Refraction.L0Tests.Events;

/// <summary>
///     Tests for <see cref="ComponentActionEvent" /> record.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Events")]
public sealed class ComponentActionEventTests
{
    /// <summary>
    ///     ComponentActionEvent can be created with action id.
    /// </summary>
    [Fact]
    [AllureFeature("ComponentActionEvent")]
    public void ComponentActionEventCanBeCreatedWithActionId()
    {
        // Arrange & Act
        ComponentActionEvent evt = new("submit");

        // Assert
        Assert.Equal("submit", evt.ActionId);
        Assert.Null(evt.Payload);
    }

    /// <summary>
    ///     ComponentActionEvent can be created with payload.
    /// </summary>
    [Fact]
    [AllureFeature("ComponentActionEvent")]
    public void ComponentActionEventCanBeCreatedWithPayload()
    {
        // Arrange
        var payload = new
        {
            Id = 42,
            Name = "Test",
        };

        // Act
        ComponentActionEvent evt = new("update", payload);

        // Assert
        Assert.Equal("update", evt.ActionId);
        Assert.NotNull(evt.Payload);
        Assert.Same(payload, evt.Payload);
    }

    /// <summary>
    ///     ComponentActionEvent implements record equality.
    /// </summary>
    [Fact]
    [AllureFeature("ComponentActionEvent")]
    public void ComponentActionEventImplementsRecordEquality()
    {
        // Arrange
        ComponentActionEvent evt1 = new("click");
        ComponentActionEvent evt2 = new("click");

        // Assert
        Assert.Equal(evt1, evt2);
    }

    /// <summary>
    ///     ComponentActionEvent is sealed record.
    /// </summary>
    [Fact]
    [AllureFeature("ComponentActionEvent")]
    public void ComponentActionEventIsSealedRecord()
    {
        // Arrange
        Type eventType = typeof(ComponentActionEvent);

        // Assert
        Assert.True(eventType.IsSealed);
        Assert.True(eventType.IsClass);
    }

    /// <summary>
    ///     ComponentActionEvent payload defaults to null.
    /// </summary>
    [Fact]
    [AllureFeature("ComponentActionEvent")]
    public void ComponentActionEventPayloadDefaultsToNull()
    {
        // Arrange & Act
        ComponentActionEvent evt = new("toggle");

        // Assert
        Assert.Null(evt.Payload);
    }
}
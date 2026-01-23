using System;

using Allure.Xunit.Attributes;

using Mississippi.Refraction.Events;


namespace Mississippi.Refraction.L0Tests.Events;

/// <summary>
///     Tests for <see cref="CommandSelectedEvent" /> record.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Events")]
public sealed class CommandSelectedEventTests
{
    /// <summary>
    ///     CommandSelectedEvent can be created with action id.
    /// </summary>
    [Fact]
    [AllureFeature("CommandSelectedEvent")]
    public void CommandSelectedEventCanBeCreatedWithActionId()
    {
        // Arrange & Act
        CommandSelectedEvent evt = new("delete");

        // Assert
        Assert.Equal("delete", evt.ActionId);
        Assert.False(evt.IsCritical);
    }

    /// <summary>
    ///     CommandSelectedEvent can be created with critical flag.
    /// </summary>
    [Fact]
    [AllureFeature("CommandSelectedEvent")]
    public void CommandSelectedEventCanBeCreatedWithCriticalFlag()
    {
        // Arrange & Act
        CommandSelectedEvent evt = new("purge", true);

        // Assert
        Assert.Equal("purge", evt.ActionId);
        Assert.True(evt.IsCritical);
    }

    /// <summary>
    ///     CommandSelectedEvent implements record equality.
    /// </summary>
    [Fact]
    [AllureFeature("CommandSelectedEvent")]
    public void CommandSelectedEventImplementsRecordEquality()
    {
        // Arrange
        CommandSelectedEvent evt1 = new("save");
        CommandSelectedEvent evt2 = new("save");

        // Assert
        Assert.Equal(evt1, evt2);
    }

    /// <summary>
    ///     CommandSelectedEvent IsCritical defaults to false.
    /// </summary>
    [Fact]
    [AllureFeature("CommandSelectedEvent")]
    public void CommandSelectedEventIsCriticalDefaultsToFalse()
    {
        // Arrange & Act
        CommandSelectedEvent evt = new("edit");

        // Assert
        Assert.False(evt.IsCritical);
    }

    /// <summary>
    ///     CommandSelectedEvent is sealed record.
    /// </summary>
    [Fact]
    [AllureFeature("CommandSelectedEvent")]
    public void CommandSelectedEventIsSealedRecord()
    {
        // Arrange
        Type eventType = typeof(CommandSelectedEvent);

        // Assert
        Assert.True(eventType.IsSealed);
        Assert.True(eventType.IsClass);
    }
}
using System;

using Allure.Xunit.Attributes;

using Mississippi.Refraction.Events;


namespace Mississippi.Refraction.L0Tests.Events;

/// <summary>
///     Tests for <see cref="ConfirmationResolvedEvent" /> record.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Events")]
public sealed class ConfirmationResolvedEventTests
{
    /// <summary>
    ///     ConfirmationResolvedEvent can be created with confirmed false.
    /// </summary>
    [Fact]
    [AllureFeature("ConfirmationResolvedEvent")]
    public void ConfirmationResolvedEventCanBeCreatedWithConfirmedFalse()
    {
        // Arrange & Act
        ConfirmationResolvedEvent evt = new(false);

        // Assert
        Assert.False(evt.Confirmed);
    }

    /// <summary>
    ///     ConfirmationResolvedEvent can be created with confirmed true.
    /// </summary>
    [Fact]
    [AllureFeature("ConfirmationResolvedEvent")]
    public void ConfirmationResolvedEventCanBeCreatedWithConfirmedTrue()
    {
        // Arrange & Act
        ConfirmationResolvedEvent evt = new(true);

        // Assert
        Assert.True(evt.Confirmed);
    }

    /// <summary>
    ///     ConfirmationResolvedEvent Confirmed property is accessible.
    /// </summary>
    [Fact]
    [AllureFeature("ConfirmationResolvedEvent")]
    public void ConfirmationResolvedEventConfirmedPropertyIsAccessible()
    {
        // Arrange
        ConfirmationResolvedEvent evt = new(true);

        // Act
        bool confirmed = evt.Confirmed;

        // Assert
        Assert.True(confirmed);
    }

    /// <summary>
    ///     ConfirmationResolvedEvent implements record equality.
    /// </summary>
    [Fact]
    [AllureFeature("ConfirmationResolvedEvent")]
    public void ConfirmationResolvedEventImplementsRecordEquality()
    {
        // Arrange
        ConfirmationResolvedEvent evt1 = new(true);
        ConfirmationResolvedEvent evt2 = new(true);

        // Assert
        Assert.Equal(evt1, evt2);
    }

    /// <summary>
    ///     ConfirmationResolvedEvent is sealed record.
    /// </summary>
    [Fact]
    [AllureFeature("ConfirmationResolvedEvent")]
    public void ConfirmationResolvedEventIsSealedRecord()
    {
        // Arrange
        Type eventType = typeof(ConfirmationResolvedEvent);

        // Assert
        Assert.True(eventType.IsSealed);
        Assert.True(eventType.IsClass);
    }
}
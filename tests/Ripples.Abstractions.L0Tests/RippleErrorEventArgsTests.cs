using System;

using Allure.Xunit.Attributes;


namespace Mississippi.Ripples.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="RippleErrorEventArgs" /> class.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Abstractions")]
[AllureSubSuite("RippleErrorEventArgs")]
public sealed class RippleErrorEventArgsTests
{
    /// <summary>
    ///     Verifies that RippleErrorEventArgs stores the exception.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorStoresException()
    {
        // Arrange
        InvalidOperationException exception = new("Test error");

        // Act
        RippleErrorEventArgs args = new(exception);

        // Assert
        Assert.Same(exception, args.Exception);
    }

    /// <summary>
    ///     Verifies that RippleErrorEventArgs throws when exception is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenExceptionIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RippleErrorEventArgs(null!));
    }

    /// <summary>
    ///     Verifies that RippleErrorEventArgs inherits from EventArgs.
    /// </summary>
    [Fact]
    [AllureFeature("Inheritance")]
    public void InheritsFromEventArgs()
    {
        // Arrange & Act
        RippleErrorEventArgs args = new(new InvalidOperationException("Test"));

        // Assert
        EventArgs eventArgs = args;
        Assert.NotNull(eventArgs);
    }
}
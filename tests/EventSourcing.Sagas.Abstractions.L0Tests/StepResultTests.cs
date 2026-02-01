using System;
using System.Collections.Generic;


namespace Mississippi.EventSourcing.Sagas.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="StepResult" /> behavior.
/// </summary>
public sealed class StepResultTests
{
    /// <summary>
    ///     Failed should allow null error message.
    /// </summary>
    [Fact]
    public void FailedAllowsNullErrorMessage()
    {
        // Act
        StepResult result = StepResult.Failed("TEST_ERROR");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("TEST_ERROR", result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     Failed should return a failed result with error details.
    /// </summary>
    [Fact]
    public void FailedReturnsFailureWithErrorDetails()
    {
        // Act
        StepResult result = StepResult.Failed("TEST_ERROR", "Test error message");

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Events);
        Assert.Equal("TEST_ERROR", result.ErrorCode);
        Assert.Equal("Test error message", result.ErrorMessage);
    }

    /// <summary>
    ///     Failed with empty error code should throw ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithEmptyErrorCodeThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => StepResult.Failed(string.Empty));
    }

    /// <summary>
    ///     Failed with null error code should throw ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithNullErrorCodeThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => StepResult.Failed(null!));
    }

    /// <summary>
    ///     Failed with whitespace error code should throw ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithWhitespaceErrorCodeThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => StepResult.Failed("   "));
    }

    /// <summary>
    ///     Succeeded should return cached instance for efficiency.
    /// </summary>
    [Fact]
    public void SucceededReturnsSameInstance()
    {
        // Act
        StepResult result1 = StepResult.Succeeded();
        StepResult result2 = StepResult.Succeeded();

        // Assert
        Assert.Same(result1, result2);
    }

    /// <summary>
    ///     Succeeded should return a successful result with no events.
    /// </summary>
    [Fact]
    public void SucceededReturnsSuccessWithNoEvents()
    {
        // Act
        StepResult result = StepResult.Succeeded();

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Events);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     Succeeded with events should return a successful result with events.
    /// </summary>
    [Fact]
    public void SucceededWithEventsReturnsSuccessWithEvents()
    {
        // Arrange
        object event1 = new
        {
            Type = "TestEvent1",
        };
        object event2 = new
        {
            Type = "TestEvent2",
        };

        // Act
        StepResult result = StepResult.Succeeded(event1, event2);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Same(event1, result.Events[0]);
        Assert.Same(event2, result.Events[1]);
    }

    /// <summary>
    ///     Succeeded with null events array should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void SucceededWithNullEventsArrayThrowsArgumentNullException()
    {
        // Arrange - explicit variable to disambiguate overload resolution
        object[]? nullArray = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => StepResult.Succeeded(nullArray!));
    }

    /// <summary>
    ///     Succeeded with null events list should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void SucceededWithNullEventsListThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => StepResult.Succeeded((IReadOnlyList<object>)null!));
    }
}
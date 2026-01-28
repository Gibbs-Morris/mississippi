using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="StepResult" />.
/// </summary>
public sealed class StepResultTests
{
    private sealed record TestEvent(string Name);

    /// <summary>
    ///     Failed returns failed result with error code.
    /// </summary>
    [Fact]
    public void FailedShouldReturnFailedResultWithErrorCode()
    {
        // Act
        StepResult result = StepResult.Failed("PAYMENT_DECLINED", "Insufficient funds");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("PAYMENT_DECLINED", result.ErrorCode);
        Assert.Equal("Insufficient funds", result.ErrorMessage);
        Assert.Empty(result.Events);
    }

    /// <summary>
    ///     Failed with null error message succeeds.
    /// </summary>
    [Fact]
    public void FailedShouldSupportNullErrorMessage()
    {
        // Act
        StepResult result = StepResult.Failed("ERROR_CODE");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR_CODE", result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     Failed with empty error code throws ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithEmptyErrorCodeShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => StepResult.Failed(string.Empty));
    }

    /// <summary>
    ///     Failed with null error code throws ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithNullErrorCodeShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => StepResult.Failed(null!));
    }

    /// <summary>
    ///     Failed with whitespace error code throws ArgumentException.
    /// </summary>
    [Fact]
    public void FailedWithWhitespaceErrorCodeShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => StepResult.Failed("   "));
    }

    /// <summary>
    ///     StepResult implements record equality.
    /// </summary>
    [Fact]
    public void StepResultShouldImplementRecordEquality()
    {
        // Arrange
        StepResult result1 = StepResult.Failed("CODE", "Message");
        StepResult result2 = StepResult.Failed("CODE", "Message");

        // Assert
        Assert.Equal(result1, result2);
    }

    /// <summary>
    ///     Succeeded returns same instance (singleton).
    /// </summary>
    [Fact]
    public void SucceededShouldReturnSameInstance()
    {
        // Act
        StepResult result1 = StepResult.Succeeded();
        StepResult result2 = StepResult.Succeeded();

        // Assert
        Assert.Same(result1, result2);
    }

    /// <summary>
    ///     Succeeded returns successful result with no events.
    /// </summary>
    [Fact]
    public void SucceededShouldReturnSuccessfulResultWithNoEvents()
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
    ///     Succeeded with null params array throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void SucceededWithNullParamsArrayShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => StepResult.Succeeded(null!));
    }

    /// <summary>
    ///     Succeeded with null IReadOnlyList throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void SucceededWithNullReadOnlyListShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => StepResult.Succeeded((IReadOnlyList<object>)null!));
    }

    /// <summary>
    ///     Succeeded with params array returns result with events.
    /// </summary>
    [Fact]
    public void SucceededWithParamsArrayShouldReturnResultWithEvents()
    {
        // Arrange
        object event1 = new TestEvent("Event1");
        object event2 = new TestEvent("Event2");

        // Act
        StepResult result = StepResult.Succeeded(event1, event2);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Contains(event1, result.Events);
        Assert.Contains(event2, result.Events);
    }

    /// <summary>
    ///     Succeeded with IReadOnlyList returns result with events.
    /// </summary>
    [Fact]
    public void SucceededWithReadOnlyListShouldReturnResultWithEvents()
    {
        // Arrange
        IReadOnlyList<object> events = [new TestEvent("Event1"), new TestEvent("Event2")];

        // Act
        StepResult result = StepResult.Succeeded(events);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
    }
}
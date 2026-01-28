namespace Mississippi.EventSourcing.Sagas.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="SagaOptionsAttribute" /> default values.
/// </summary>
public sealed class SagaOptionsAttributeTests
{
    /// <summary>
    ///     Default CompensationStrategy should be Immediate.
    /// </summary>
    [Fact]
    public void DefaultCompensationStrategyIsImmediate()
    {
        // Act
        SagaOptionsAttribute attribute = new();

        // Assert
        Assert.Equal(CompensationStrategy.Immediate, attribute.CompensationStrategy);
    }

    /// <summary>
    ///     Default MaxRetries should be 3.
    /// </summary>
    [Fact]
    public void DefaultMaxRetriesIsThree()
    {
        // Act
        SagaOptionsAttribute attribute = new();

        // Assert
        Assert.Equal(3, attribute.MaxRetries);
    }

    /// <summary>
    ///     Default DefaultStepTimeout should be null.
    /// </summary>
    [Fact]
    public void DefaultStepTimeoutIsNull()
    {
        // Act
        SagaOptionsAttribute attribute = new();

        // Assert
        Assert.Null(attribute.DefaultStepTimeout);
    }

    /// <summary>
    ///     Default TimeoutBehavior should be FailAndCompensate.
    /// </summary>
    [Fact]
    public void DefaultTimeoutBehaviorIsFailAndCompensate()
    {
        // Act
        SagaOptionsAttribute attribute = new();

        // Assert
        Assert.Equal(TimeoutBehavior.FailAndCompensate, attribute.TimeoutBehavior);
    }

    /// <summary>
    ///     Properties should be settable.
    /// </summary>
    [Fact]
    public void PropertiesAreSettable()
    {
        // Act
        SagaOptionsAttribute attribute = new()
        {
            CompensationStrategy = CompensationStrategy.RetryThenCompensate,
            TimeoutBehavior = TimeoutBehavior.AwaitIntervention,
            MaxRetries = 5,
            DefaultStepTimeout = "00:05:00",
        };

        // Assert
        Assert.Equal(CompensationStrategy.RetryThenCompensate, attribute.CompensationStrategy);
        Assert.Equal(TimeoutBehavior.AwaitIntervention, attribute.TimeoutBehavior);
        Assert.Equal(5, attribute.MaxRetries);
        Assert.Equal("00:05:00", attribute.DefaultStepTimeout);
    }
}
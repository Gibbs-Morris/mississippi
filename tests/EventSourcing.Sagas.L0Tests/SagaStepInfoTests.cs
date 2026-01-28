using System;

using Mississippi.EventSourcing.Sagas;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="SagaStepInfo" />.
/// </summary>
public sealed class SagaStepInfoTests
{
    /// <summary>
    ///     Verifies required properties are accessible when set.
    /// </summary>
    [Fact]
    public void SagaStepInfoShouldExposeRequiredProperties()
    {
        // Act
        SagaStepInfo info = new()
        {
            Name = "ProcessPayment",
            Order = 2,
            StepType = typeof(SagaStepInfoTests),
        };

        // Assert
        Assert.Equal("ProcessPayment", info.Name);
        Assert.Equal(2, info.Order);
        Assert.Equal(typeof(SagaStepInfoTests), info.StepType);
    }

    /// <summary>
    ///     Verifies optional properties are accessible when set.
    /// </summary>
    [Fact]
    public void SagaStepInfoShouldExposeOptionalProperties()
    {
        // Act
        SagaStepInfo info = new()
        {
            Name = "SendEmail",
            Order = 3,
            StepType = typeof(SagaStepInfoTests),
            Timeout = TimeSpan.FromMinutes(5),
            CompensationType = typeof(string),
        };

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), info.Timeout);
        Assert.Equal(typeof(string), info.CompensationType);
    }

    /// <summary>
    ///     Verifies Timeout returns null when not set.
    /// </summary>
    [Fact]
    public void SagaStepInfoShouldReturnNullForUnsetTimeout()
    {
        // Act
        SagaStepInfo info = new()
        {
            Name = "Step1",
            Order = 1,
            StepType = typeof(object),
            Timeout = null,
        };

        // Assert
        Assert.Null(info.Timeout);
    }

    /// <summary>
    ///     Verifies CompensationType returns null when not set.
    /// </summary>
    [Fact]
    public void SagaStepInfoShouldReturnNullForUnsetCompensationType()
    {
        // Act
        SagaStepInfo info = new()
        {
            Name = "Step1",
            Order = 1,
            StepType = typeof(object),
            CompensationType = null,
        };

        // Assert
        Assert.Null(info.CompensationType);
    }
}

using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="SagaStepAttribute" /> behavior.
/// </summary>
public sealed class SagaStepAttributeTests
{
    /// <summary>
    ///     Constructor should set Order property.
    /// </summary>
    [Fact]
    public void ConstructorSetsOrderProperty()
    {
        // Act
        SagaStepAttribute attribute = new(5);

        // Assert
        Assert.Equal(5, attribute.Order);
    }

    /// <summary>
    ///     Constructor should throw when order is less than 1.
    /// </summary>
    /// <param name="invalidOrder">The invalid order value to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ConstructorThrowsWhenOrderLessThanOne(
        int invalidOrder
    )
    {
        // Act & Assert
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(() => new SagaStepAttribute(invalidOrder));
        Assert.Equal("order", exception.ParamName);
    }

    /// <summary>
    ///     Timeout property should be null by default.
    /// </summary>
    [Fact]
    public void TimeoutPropertyIsNullByDefault()
    {
        // Act
        SagaStepAttribute attribute = new(1);

        // Assert
        Assert.Null(attribute.Timeout);
    }

    /// <summary>
    ///     Timeout property should be settable.
    /// </summary>
    [Fact]
    public void TimeoutPropertyIsSettable()
    {
        // Act
        SagaStepAttribute attribute = new(1)
        {
            Timeout = "00:10:00",
        };

        // Assert
        Assert.Equal("00:10:00", attribute.Timeout);
    }
}
using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="SagaCompensationAttribute" /> behavior.
/// </summary>
public sealed class SagaCompensationAttributeTests
{
    /// <summary>
    ///     A sample step type for testing.
    /// </summary>
    private static class SampleStep
    {
        /// <summary>
        ///     The step order for testing.
        /// </summary>
        public const int Order = 1;
    }

    /// <summary>
    ///     Constructor should set ForStep property.
    /// </summary>
    [Fact]
    public void ConstructorSetsForStepProperty()
    {
        // Arrange - verify the sample step is meaningful
        Assert.True(SampleStep.Order > 0);

        // Act
        SagaCompensationAttribute attribute = new(typeof(SampleStep));

        // Assert
        Assert.Equal(typeof(SampleStep), attribute.ForStep);
    }

    /// <summary>
    ///     Constructor should throw when forStep is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenForStepIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SagaCompensationAttribute(null!));
    }
}
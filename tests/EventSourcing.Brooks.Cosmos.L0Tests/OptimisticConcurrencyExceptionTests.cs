using System;



namespace Mississippi.EventSourcing.Brooks.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="OptimisticConcurrencyException" /> behavior.
/// </summary>
public sealed class OptimisticConcurrencyExceptionTests
{
    /// <summary>
    ///     Default constructor should create exception with null message.
    /// </summary>
    [Fact]
        public void DefaultConstructorCreatesExceptionWithNullMessage()
    {
        // Arrange & Act
        OptimisticConcurrencyException exception = new();

        // Assert
        Assert.Null(exception.InnerException);
    }

    /// <summary>
    ///     Exception should derive from Exception base class.
    /// </summary>
    [Fact]
        public void ExceptionDerivesFromExceptionBaseClass()
    {
        // Arrange & Act
        OptimisticConcurrencyException exception = new("test");

        // Assert
        Assert.IsType<OptimisticConcurrencyException>(exception, false);
    }

    /// <summary>
    ///     Message and inner exception constructor should set both properties.
    /// </summary>
    [Fact]
        public void MessageAndInnerExceptionConstructorSetsBothProperties()
    {
        // Arrange
        const string message = "Concurrency conflict detected";
        InvalidOperationException innerException = new("Inner error");

        // Act
        OptimisticConcurrencyException exception = new(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    /// <summary>
    ///     Message constructor should set the message.
    /// </summary>
    [Fact]
        public void MessageConstructorSetsMessage()
    {
        // Arrange
        const string message = "Concurrency conflict detected";

        // Act
        OptimisticConcurrencyException exception = new(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }
}
using System;

using Mississippi.EventSourcing.Sagas;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="SagaContext" />.
/// </summary>
public sealed class SagaContextTests
{
    /// <summary>
    ///     Verifies all SagaContext properties are accessible when set.
    /// </summary>
    [Fact]
    public void SagaContextShouldExposeAllPropertiesWhenSet()
    {
        // Arrange
        Guid sagaId = Guid.NewGuid();
        string correlationId = "test-correlation";
        string sagaName = "TestSaga";
        DateTimeOffset startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        // Act
        SagaContext context = new()
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
            SagaName = sagaName,
            StartedAt = startedAt,
            Attempt = 3,
        };

        // Assert
        Assert.Equal(sagaId, context.SagaId);
        Assert.Equal(correlationId, context.CorrelationId);
        Assert.Equal(sagaName, context.SagaName);
        Assert.Equal(startedAt, context.StartedAt);
        Assert.Equal(3, context.Attempt);
    }

    /// <summary>
    ///     Verifies Attempt defaults to 1 when not specified.
    /// </summary>
    [Fact]
    public void SagaContextShouldDefaultAttemptToOne()
    {
        // Act
        SagaContext context = new()
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "c1",
            SagaName = "S1",
            StartedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.Equal(1, context.Attempt);
    }
}

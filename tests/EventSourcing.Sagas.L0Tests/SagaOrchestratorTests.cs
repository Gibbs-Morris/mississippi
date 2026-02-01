using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Commands;
using Mississippi.EventSourcing.Sagas.Abstractions.Projections;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

using Moq;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="SagaOrchestrator" />.
/// </summary>
public sealed class SagaOrchestratorTests
{
    /// <summary>
    ///     GetStatusAsync returns null (not yet implemented).
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task GetStatusAsyncShouldReturnNull()
    {
        // Arrange
        Mock<IAggregateGrainFactory> factoryMock = new();
        SagaOrchestrator sut = new(factoryMock.Object);
        Guid sagaId = Guid.NewGuid();

        // Act
        SagaStatusProjection? result = await sut.GetStatusAsync(sagaId);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     StartAsync calls aggregate grain with StartSagaCommand.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task StartAsyncShouldCallAggregateGrain()
    {
        // Arrange
        Mock<IAggregateGrainFactory> factoryMock = new();
        Mock<IGenericAggregateGrain<TestSagaState>> grainMock = new();
        Guid sagaId = Guid.NewGuid();
        string entityId = sagaId.ToString();
        TestSagaInput input = new("Order123");
        factoryMock.Setup(f => f.GetGenericAggregate<TestSagaState>(entityId)).Returns(grainMock.Object);
        grainMock.Setup(g => g.ExecuteAsync(It.IsAny<StartSagaCommand<TestSagaInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());
        SagaOrchestrator sut = new(factoryMock.Object);

        // Act
        await sut.StartAsync<TestSagaState, TestSagaInput>(sagaId, input, "corr-id");

        // Assert
        factoryMock.Verify(f => f.GetGenericAggregate<TestSagaState>(entityId), Times.Once);
        grainMock.Verify(
            g => g.ExecuteAsync(
                It.Is<StartSagaCommand<TestSagaInput>>(c => (c.SagaId == sagaId) && (c.Input == input) && (c.CorrelationId == "corr-id")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     StartAsync supports null correlation ID.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task StartAsyncShouldSupportNullCorrelationId()
    {
        // Arrange
        Mock<IAggregateGrainFactory> factoryMock = new();
        Mock<IGenericAggregateGrain<TestSagaState>> grainMock = new();
        Guid sagaId = Guid.NewGuid();
        TestSagaInput input = new("Order123");
        factoryMock.Setup(f => f.GetGenericAggregate<TestSagaState>(sagaId.ToString())).Returns(grainMock.Object);
        grainMock.Setup(g => g.ExecuteAsync(It.IsAny<StartSagaCommand<TestSagaInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());
        SagaOrchestrator sut = new(factoryMock.Object);

        // Act
        await sut.StartAsync<TestSagaState, TestSagaInput>(sagaId, input);

        // Assert
        grainMock.Verify(
            g => g.ExecuteAsync(
                It.Is<StartSagaCommand<TestSagaInput>>(c => c.CorrelationId == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     StartAsync throws when aggregate grain returns failure.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task StartAsyncShouldThrowWhenGrainReturnsFails()
    {
        // Arrange
        Mock<IAggregateGrainFactory> factoryMock = new();
        Mock<IGenericAggregateGrain<TestSagaState>> grainMock = new();
        Guid sagaId = Guid.NewGuid();
        TestSagaInput input = new("Order123");
        factoryMock.Setup(f => f.GetGenericAggregate<TestSagaState>(sagaId.ToString())).Returns(grainMock.Object);
        grainMock.Setup(g => g.ExecuteAsync(It.IsAny<StartSagaCommand<TestSagaInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("SAGA_ALREADY_STARTED", "Saga already started"));
        SagaOrchestrator sut = new(factoryMock.Object);

        // Act & Assert
        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.StartAsync<TestSagaState, TestSagaInput>(sagaId, input));
        Assert.Contains("SAGA_ALREADY_STARTED", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     StartAsync throws when input is null.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task StartAsyncShouldThrowWhenInputIsNull()
    {
        // Arrange
        Mock<IAggregateGrainFactory> factoryMock = new();
        SagaOrchestrator sut = new(factoryMock.Object);
        Guid sagaId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.StartAsync<TestSagaState, TestSagaInput>(sagaId, null!));
    }
}
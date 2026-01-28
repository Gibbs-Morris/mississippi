using System;
using System.Collections.Generic;

using Microsoft.Extensions.Time.Testing;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Commands;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

using Moq;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="StartSagaCommandHandler{TInput, TSaga}" />.
/// </summary>
public sealed class StartSagaCommandHandlerTests
{
    /// <summary>
    ///     Handle fails for various running phases.
    /// </summary>
    [Theory]
    [InlineData(SagaPhase.Running)]
    [InlineData(SagaPhase.Compensating)]
    [InlineData(SagaPhase.Completed)]
    [InlineData(SagaPhase.Failed)]
    public void HandleShouldFailForNonNotStartedPhases(
        SagaPhase phase
    )
    {
        // Arrange
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        FakeTimeProvider timeProvider = new();
        StartSagaCommandHandler<TestSagaInput, TestSagaState> sut = new(registryMock.Object, timeProvider);
        StartSagaCommand<TestSagaInput> command = new(new("Order123"));
        TestSagaState state = new()
        {
            Phase = phase,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = sut.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("SAGA_ALREADY_STARTED", result.ErrorCode);
    }

    /// <summary>
    ///     Handle fails when no steps are defined.
    /// </summary>
    [Fact]
    public void HandleShouldFailWhenNoStepsDefined()
    {
        // Arrange
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        FakeTimeProvider timeProvider = new();
        registryMock.Setup(r => r.Steps).Returns([]);
        StartSagaCommandHandler<TestSagaInput, TestSagaState> sut = new(registryMock.Object, timeProvider);
        StartSagaCommand<TestSagaInput> command = new(new("Order123"));

        // Act
        OperationResult<IReadOnlyList<object>> result = sut.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_SAGA_STEPS", result.ErrorCode);
    }

    /// <summary>
    ///     Handle fails when saga already started.
    /// </summary>
    [Fact]
    public void HandleShouldFailWhenSagaAlreadyStarted()
    {
        // Arrange
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        FakeTimeProvider timeProvider = new();
        StartSagaCommandHandler<TestSagaInput, TestSagaState> sut = new(registryMock.Object, timeProvider);
        StartSagaCommand<TestSagaInput> command = new(new("Order123"));
        TestSagaState existingState = new()
        {
            Phase = SagaPhase.Running,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = sut.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("SAGA_ALREADY_STARTED", result.ErrorCode);
    }

    /// <summary>
    ///     Handle returns saga started and step started events on success.
    /// </summary>
    [Fact]
    public void HandleShouldReturnSagaStartedAndStepStartedEvents()
    {
        // Arrange
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        FakeTimeProvider timeProvider = new();
        DateTimeOffset now = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        timeProvider.SetUtcNow(now);
        registryMock.Setup(r => r.Steps)
            .Returns(
            [
                new SagaStepInfo
                {
                    Name = "ValidateOrder",
                    Order = 1,
                    StepType = typeof(object),
                },
                new SagaStepInfo
                {
                    Name = "ProcessPayment",
                    Order = 2,
                    StepType = typeof(object),
                },
            ]);
        registryMock.Setup(r => r.StepHash).Returns("testhash123");
        StartSagaCommandHandler<TestSagaInput, TestSagaState> sut = new(registryMock.Object, timeProvider);
        StartSagaCommand<TestSagaInput> command = new(new("Order123"), "corr-id");

        // Act
        OperationResult<IReadOnlyList<object>> result = sut.Handle(command, null);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Value!.Count);
        Assert.IsType<SagaStartedEvent>(result.Value[0]);
        Assert.IsType<SagaStepStartedEvent>(result.Value[1]);
        SagaStartedEvent startedEvent = (SagaStartedEvent)result.Value[0];
        Assert.Equal("TestSagaState", startedEvent.SagaType);
        Assert.Equal("testhash123", startedEvent.StepHash);
        Assert.Equal("corr-id", startedEvent.CorrelationId);
        Assert.Equal(now, startedEvent.Timestamp);
        SagaStepStartedEvent stepEvent = (SagaStepStartedEvent)result.Value[1];
        Assert.Equal("ValidateOrder", stepEvent.StepName);
        Assert.Equal(1, stepEvent.StepOrder);
    }

    /// <summary>
    ///     Handle succeeds when state is NotStarted.
    /// </summary>
    [Fact]
    public void HandleShouldSucceedWhenStateIsNotStarted()
    {
        // Arrange
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        FakeTimeProvider timeProvider = new();
        registryMock.Setup(r => r.Steps)
            .Returns(
            [
                new SagaStepInfo
                {
                    Name = "Step1",
                    Order = 1,
                    StepType = typeof(object),
                },
            ]);
        registryMock.Setup(r => r.StepHash).Returns("hash");
        StartSagaCommandHandler<TestSagaInput, TestSagaState> sut = new(registryMock.Object, timeProvider);
        StartSagaCommand<TestSagaInput> command = new(new("Order123"));
        TestSagaState state = new()
        {
            Phase = SagaPhase.NotStarted,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = sut.Handle(command, state);

        // Assert
        Assert.True(result.Success);
    }

    /// <summary>
    ///     Handle supports null correlation ID.
    /// </summary>
    [Fact]
    public void HandleShouldSupportNullCorrelationId()
    {
        // Arrange
        Mock<ISagaStepRegistry<TestSagaState>> registryMock = new();
        FakeTimeProvider timeProvider = new();
        registryMock.Setup(r => r.Steps)
            .Returns(
            [
                new SagaStepInfo
                {
                    Name = "Step1",
                    Order = 1,
                    StepType = typeof(object),
                },
            ]);
        registryMock.Setup(r => r.StepHash).Returns("hash");
        StartSagaCommandHandler<TestSagaInput, TestSagaState> sut = new(registryMock.Object, timeProvider);
        StartSagaCommand<TestSagaInput> command = new(new("Order123"));

        // Act
        OperationResult<IReadOnlyList<object>> result = sut.Handle(command, null);

        // Assert
        Assert.True(result.Success);
        SagaStartedEvent startedEvent = (SagaStartedEvent)result.Value![0];
        Assert.Null(startedEvent.CorrelationId);
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;

using Moq;

using EncoderFallbackException = System.Text.EncoderFallbackException;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="StartSagaCommandHandler{TSaga,TInput}" />.
/// </summary>
public sealed class StartSagaCommandHandlerTests
{
    private sealed class CreditStep : ISagaStep<TestSagaState>
    {
        public Task<StepResult> ExecuteAsync(
            TestSagaState state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(StepResult.Succeeded());
    }

    private sealed class DebitStep
        : ISagaStep<TestSagaState>,
          ICompensatable<TestSagaState>
    {
        public Task<CompensationResult> CompensateAsync(
            TestSagaState state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(CompensationResult.Succeeded());

        public Task<StepResult> ExecuteAsync(
            TestSagaState state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(StepResult.Succeeded());
    }

    private sealed record TestInput(string TransferId);

    /// <summary>
    ///     Verifies the handler fails when no steps are registered.
    /// </summary>
    [Fact]
    public void HandleFailsWhenNoStepsRegistered()
    {
        FakeTimeProvider timeProvider = new();
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new SagaStepInfoProvider<TestSagaState>(Array.Empty<SagaStepInfo>()),
            timeProvider);
        StartSagaCommand<TestInput> command = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new("transfer-1"),
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies the handler fails when the saga is already started.
    /// </summary>
    [Fact]
    public void HandleFailsWhenSagaAlreadyStarted()
    {
        FakeTimeProvider timeProvider = new();
        IReadOnlyList<SagaStepInfo> steps =
        [
            new(0, "Debit", typeof(DebitStep), true),
        ];
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new SagaStepInfoProvider<TestSagaState>(steps),
            timeProvider);
        StartSagaCommand<TestInput> command = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new("transfer-1"),
        };
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies runtime interruption and cancellation are not classified as invalid configuration.
    /// </summary>
    /// <param name="isCancellation">Whether to inject cancellation instead of an interruption.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandlePropagatesMetadataInterruption(
        bool isCancellation
    )
    {
        Exception failure = isCancellation ? new OperationCanceledException() : new ThreadInterruptedException();
        Mock<ISagaStepInfoProvider<TestSagaState>> metadata = new();
        metadata.SetupGet(p => p.Steps).Throws(failure);
        StartSagaCommandHandler<TestSagaState, string> handler = new(metadata.Object, new FakeTimeProvider());
        Assert.Throws(
            failure.GetType(),
            () => handler.Handle(
                new()
                {
                    SagaId = Guid.NewGuid(),
                    Input = "transfer",
                },
                null));
    }

    /// <summary>
    ///     Verifies invalid workflow text is rejected at the command boundary without starting the saga.
    /// </summary>
    [Fact]
    public void HandleRejectsInvalidWorkflowMetadata()
    {
        Mock<ILogger<StartSagaCommandHandler<TestSagaState, string>>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        StartSagaCommandHandler<TestSagaState, string> handler = new(
            new SagaStepInfoProvider<TestSagaState>([new(0, new((char)0xD800, 1), typeof(DebitStep), true)]),
            new FakeTimeProvider(),
            logger.Object);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(
            new()
            {
                SagaId = Guid.NewGuid(),
                Input = "transfer",
            },
            null);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
        Assert.Equal("The registered saga workflow metadata is invalid.", result.ErrorMessage);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(id => id.Id == 1),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<EncoderFallbackException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies the handler emits a saga-started event for new sagas.
    /// </summary>
    [Fact]
    public void HandleReturnsSagaStartedEvent()
    {
        DateTimeOffset now = new(2025, 2, 10, 9, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        IReadOnlyList<SagaStepInfo> steps =
        [
            new(0, "Debit", typeof(DebitStep), true),
            new(1, "Credit", typeof(CreditStep), false),
        ];
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new SagaStepInfoProvider<TestSagaState>(steps),
            timeProvider);
        StartSagaCommand<TestInput> command = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new("transfer-1"),
            CorrelationId = "corr-123",
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);
        Assert.True(result.Success);
        Assert.Equal(2, result.Value.Count);
        SagaStartedEvent started = Assert.IsType<SagaStartedEvent>(result.Value[0]);
        SagaInputProvided<TestInput> inputProvided = Assert.IsType<SagaInputProvided<TestInput>>(result.Value[1]);
        Assert.Equal(command.SagaId, started.SagaId);
        Assert.Equal(command.CorrelationId, started.CorrelationId);
        Assert.Equal(now, started.StartedAt);
        Assert.Equal("7DE21E6C320F5F9137F8B13B91B6ACEF2204ADF5A5AAF54E469956A82E898B60", started.StepHash);
        Assert.Equal(command.SagaId, inputProvided.SagaId);
        Assert.Equal(command.Input, inputProvided.Input);
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="SagaStepBase{TSaga}" />.
/// </summary>
public sealed class SagaStepBaseTests
{
    private static TestSagaContext CreateContext() =>
        new()
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "test-correlation",
            SagaName = "TestSaga",
            StartedAt = DateTimeOffset.UtcNow,
            Attempt = 1,
        };

    /// <summary>
    ///     Test step that captures the cancellation token.
    /// </summary>
    private sealed class CancellationCapturingStep : SagaStepBase<TestSagaState>
    {
        public CancellationToken CapturedToken { get; private set; }

        public override Task<StepResult> ExecuteAsync(
            ISagaContext context,
            TestSagaState state,
            CancellationToken cancellationToken
        )
        {
            CapturedToken = cancellationToken;
            return Task.FromResult(StepResult.Succeeded());
        }
    }

    /// <summary>
    ///     Test step that captures the context.
    /// </summary>
    private sealed class ContextCapturingStep : SagaStepBase<TestSagaState>
    {
        public ISagaContext? CapturedContext { get; private set; }

        public override Task<StepResult> ExecuteAsync(
            ISagaContext context,
            TestSagaState state,
            CancellationToken cancellationToken
        )
        {
            CapturedContext = context;
            return Task.FromResult(StepResult.Succeeded());
        }
    }

    /// <summary>
    ///     Test step that captures the state.
    /// </summary>
    private sealed class StateCapturingStep : SagaStepBase<TestSagaState>
    {
        public TestSagaState? CapturedState { get; private set; }

        public override Task<StepResult> ExecuteAsync(
            ISagaContext context,
            TestSagaState state,
            CancellationToken cancellationToken
        )
        {
            CapturedState = state;
            return Task.FromResult(StepResult.Succeeded());
        }
    }

    /// <summary>
    ///     Test implementation of <see cref="ISagaContext" />.
    /// </summary>
    private sealed class TestSagaContext : ISagaContext
    {
        public int Attempt { get; init; }

        public string CorrelationId { get; init; } = string.Empty;

        public Guid SagaId { get; init; }

        public string SagaName { get; init; } = string.Empty;

        public DateTimeOffset StartedAt { get; init; }
    }

    /// <summary>
    ///     Test saga step that returns success.
    /// </summary>
    private sealed class TestSagaStep : SagaStepBase<TestSagaState>
    {
        public override Task<StepResult> ExecuteAsync(
            ISagaContext context,
            TestSagaState state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(StepResult.Succeeded());
    }

    /// <summary>
    ///     ExecuteAsync receives current state.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncShouldReceiveCurrentState()
    {
        // Arrange
        StateCapturingStep step = new();
        TestSagaState state = new()
        {
            CorrelationId = "test-corr",
        };
        TestSagaContext context = CreateContext();

        // Act
        await step.ExecuteAsync(context, state, CancellationToken.None);

        // Assert
        Assert.Equal("test-corr", step.CapturedState?.CorrelationId);
    }

    /// <summary>
    ///     ExecuteAsync receives saga context.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncShouldReceiveSagaContext()
    {
        // Arrange
        ContextCapturingStep step = new();
        TestSagaState state = new();
        TestSagaContext context = CreateContext();

        // Act
        await step.ExecuteAsync(context, state, CancellationToken.None);

        // Assert
        Assert.Equal(context.SagaId, step.CapturedContext?.SagaId);
        Assert.Equal(context.CorrelationId, step.CapturedContext?.CorrelationId);
    }

    /// <summary>
    ///     ExecuteAsync respects cancellation token.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncShouldRespectCancellationToken()
    {
        // Arrange
        CancellationCapturingStep step = new();
        TestSagaState state = new();
        TestSagaContext context = CreateContext();
        using CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;

        // Act
        await step.ExecuteAsync(context, state, token);

        // Assert
        Assert.Equal(token, step.CapturedToken);
    }

    /// <summary>
    ///     ExecuteAsync returns StepResult.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncShouldReturnStepResult()
    {
        // Arrange
        TestSagaStep step = new();
        TestSagaState state = new();
        TestSagaContext context = CreateContext();

        // Act
        StepResult result = await step.ExecuteAsync(context, state, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
    }

    /// <summary>
    ///     Derived step class can be instantiated.
    /// </summary>
    [Fact]
    public void SagaStepBaseDerivedClassShouldBeInstantiable()
    {
        // Act
        TestSagaStep step = new();

        // Assert
        Assert.NotNull(step);
    }
}
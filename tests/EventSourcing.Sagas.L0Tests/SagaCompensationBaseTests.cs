using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="SagaCompensationBase{TSaga}" />.
/// </summary>
public sealed class SagaCompensationBaseTests
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
    ///     Test compensation that captures the cancellation token.
    /// </summary>
    private sealed class CancellationCapturingCompensation : SagaCompensationBase<TestSagaState>
    {
        public CancellationToken CapturedToken { get; private set; }

        public override Task<CompensationResult> CompensateAsync(
            ISagaContext context,
            TestSagaState state,
            CancellationToken cancellationToken
        )
        {
            CapturedToken = cancellationToken;
            return Task.FromResult(CompensationResult.Succeeded());
        }
    }

    /// <summary>
    ///     Test compensation that captures the context.
    /// </summary>
    private sealed class ContextCapturingCompensation : SagaCompensationBase<TestSagaState>
    {
        public ISagaContext? CapturedContext { get; private set; }

        public override Task<CompensationResult> CompensateAsync(
            ISagaContext context,
            TestSagaState state,
            CancellationToken cancellationToken
        )
        {
            CapturedContext = context;
            return Task.FromResult(CompensationResult.Succeeded());
        }
    }

    /// <summary>
    ///     Test compensation that returns skipped.
    /// </summary>
    private sealed class SkippingCompensation : SagaCompensationBase<TestSagaState>
    {
        public override Task<CompensationResult> CompensateAsync(
            ISagaContext context,
            TestSagaState state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(CompensationResult.Skipped());
    }

    /// <summary>
    ///     Test compensation that captures the state.
    /// </summary>
    private sealed class StateCapturingCompensation : SagaCompensationBase<TestSagaState>
    {
        public TestSagaState? CapturedState { get; private set; }

        public override Task<CompensationResult> CompensateAsync(
            ISagaContext context,
            TestSagaState state,
            CancellationToken cancellationToken
        )
        {
            CapturedState = state;
            return Task.FromResult(CompensationResult.Succeeded());
        }
    }

    /// <summary>
    ///     Test saga compensation that returns success.
    /// </summary>
    private sealed class TestSagaCompensation : SagaCompensationBase<TestSagaState>
    {
        public override Task<CompensationResult> CompensateAsync(
            ISagaContext context,
            TestSagaState state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(CompensationResult.Succeeded());
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
    ///     CompensateAsync receives current state.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task CompensateAsyncShouldReceiveCurrentState()
    {
        // Arrange
        StateCapturingCompensation compensation = new();
        TestSagaState state = new()
        {
            CorrelationId = "test-corr",
        };
        TestSagaContext context = CreateContext();

        // Act
        await compensation.CompensateAsync(context, state, CancellationToken.None);

        // Assert
        Assert.Equal("test-corr", compensation.CapturedState?.CorrelationId);
    }

    /// <summary>
    ///     CompensateAsync receives saga context.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task CompensateAsyncShouldReceiveSagaContext()
    {
        // Arrange
        ContextCapturingCompensation compensation = new();
        TestSagaState state = new();
        TestSagaContext context = CreateContext();

        // Act
        await compensation.CompensateAsync(context, state, CancellationToken.None);

        // Assert
        Assert.Equal(context.SagaId, compensation.CapturedContext?.SagaId);
        Assert.Equal(context.CorrelationId, compensation.CapturedContext?.CorrelationId);
    }

    /// <summary>
    ///     CompensateAsync respects cancellation token.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task CompensateAsyncShouldRespectCancellationToken()
    {
        // Arrange
        CancellationCapturingCompensation compensation = new();
        TestSagaState state = new();
        TestSagaContext context = CreateContext();
        using CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;

        // Act
        await compensation.CompensateAsync(context, state, token);

        // Assert
        Assert.Equal(token, compensation.CapturedToken);
    }

    /// <summary>
    ///     CompensateAsync returns CompensationResult.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task CompensateAsyncShouldReturnCompensationResult()
    {
        // Arrange
        TestSagaCompensation compensation = new();
        TestSagaState state = new();
        TestSagaContext context = CreateContext();

        // Act
        CompensationResult result = await compensation.CompensateAsync(context, state, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
    }

    /// <summary>
    ///     CompensateAsync can return skipped result.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task CompensateAsyncShouldSupportSkippedResult()
    {
        // Arrange
        SkippingCompensation compensation = new();
        TestSagaState state = new();
        TestSagaContext context = CreateContext();

        // Act
        CompensationResult result = await compensation.CompensateAsync(context, state, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.WasSkipped);
    }

    /// <summary>
    ///     Derived compensation class can be instantiated.
    /// </summary>
    [Fact]
    public void SagaCompensationBaseDerivedClassShouldBeInstantiable()
    {
        // Act
        TestSagaCompensation compensation = new();

        // Assert
        Assert.NotNull(compensation);
    }
}
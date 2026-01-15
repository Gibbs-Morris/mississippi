using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.Aggregates.Api.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateServiceBase{TAggregate}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates API")]
[AllureSubSuite("Service Base")]
public sealed class AggregateServiceBaseTests
{
    private readonly Mock<IAggregateGrainFactory> mockFactory = new();

    private static ILogger<AggregateServiceBase<TestAggregate>> NullServiceLogger =>
        NullLogger<AggregateServiceBase<TestAggregate>>.Instance;

    /// <summary>
    ///     Testable implementation of AggregateServiceBase for testing.
    /// </summary>
    private sealed class TestableAggregateService : AggregateServiceBase<TestAggregate>
    {
        public TestableAggregateService(
            IAggregateGrainFactory aggregateGrainFactory,
            ILogger<AggregateServiceBase<TestAggregate>> logger
        )
            : base(aggregateGrainFactory, logger)
        {
        }

        public IAggregateGrainFactory ExposedAggregateGrainFactory => AggregateGrainFactory;

        public ILogger<AggregateServiceBase<TestAggregate>> ExposedLogger => Logger;

        public Task<OperationResult> TestExecuteCommandAsync<TCommand>(
            string entityId,
            TCommand command,
            CancellationToken cancellationToken = default
        )
            where TCommand : class =>
            ExecuteCommandAsync(entityId, command, cancellationToken);
    }

    /// <summary>
    ///     Tracking implementation to verify hook methods are called.
    /// </summary>
    private sealed class TrackingAggregateService : AggregateServiceBase<TestAggregate>
    {
        public TrackingAggregateService(
            IAggregateGrainFactory aggregateGrainFactory,
            ILogger<AggregateServiceBase<TestAggregate>> logger
        )
            : base(aggregateGrainFactory, logger)
        {
        }

        public string? AfterEntityId { get; private set; }

        public OperationResult? AfterResult { get; private set; }

        public string? BeforeEntityId { get; private set; }

        public bool OnAfterExecuteCalled { get; private set; }

        public bool OnBeforeExecuteCalled { get; private set; }

        public Task<OperationResult> TestExecuteCommandAsync<TCommand>(
            string entityId,
            TCommand command,
            CancellationToken cancellationToken = default
        )
            where TCommand : class =>
            ExecuteCommandAsync(entityId, command, cancellationToken);

        protected override Task OnAfterExecuteAsync<TCommand>(
            string entityId,
            TCommand command,
            OperationResult result,
            CancellationToken cancellationToken = default
        )
        {
            OnAfterExecuteCalled = true;
            AfterEntityId = entityId;
            AfterResult = result;
            return Task.CompletedTask;
        }

        protected override Task OnBeforeExecuteAsync<TCommand>(
            string entityId,
            TCommand command,
            CancellationToken cancellationToken = default
        )
        {
            OnBeforeExecuteCalled = true;
            BeforeEntityId = entityId;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Tests that AggregateGrainFactory property returns injected factory.
    /// </summary>
    [Fact(DisplayName = "AggregateGrainFactory property returns injected factory")]
    public void AggregateGrainFactoryPropertyReturnsInjectedFactory()
    {
        // Arrange & Act
        TestableAggregateService service = new(mockFactory.Object, NullServiceLogger);

        // Assert
        Assert.Same(mockFactory.Object, service.ExposedAggregateGrainFactory);
    }

    /// <summary>
    ///     Tests that constructor throws when aggregateGrainFactory is null.
    /// </summary>
    [Fact(DisplayName = "Constructor throws when aggregateGrainFactory is null")]
    public void ConstructorThrowsWhenAggregateGrainFactoryIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateService(null!, NullServiceLogger));
    }

    /// <summary>
    ///     Tests that constructor throws when logger is null.
    /// </summary>
    [Fact(DisplayName = "Constructor throws when logger is null")]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateService(mockFactory.Object, null!));
    }

    /// <summary>
    ///     Tests that ExecuteCommandAsync calls grain and returns success.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteCommandAsync calls grain and returns success")]
    public async Task ExecuteCommandAsyncCallsGrainAndReturnsSuccessAsync()
    {
        // Arrange
        TestCommand command = new("test");
        OperationResult expectedResult = OperationResult.Ok();
        Mock<IGenericAggregateGrain<TestAggregate>> mockGrain = new();
        mockGrain.Setup(g => g.ExecuteAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);
        mockFactory.Setup(f => f.GetGenericAggregate<TestAggregate>("entity-1")).Returns(mockGrain.Object);
        TestableAggregateService service = new(mockFactory.Object, NullServiceLogger);

        // Act
        OperationResult result = await service.TestExecuteCommandAsync("entity-1", command);

        // Assert
        Assert.True(result.Success);
        mockFactory.Verify(f => f.GetGenericAggregate<TestAggregate>("entity-1"), Times.Once);
        mockGrain.Verify(g => g.ExecuteAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Tests that ExecuteCommandAsync returns failure result from grain.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteCommandAsync returns failure result from grain")]
    public async Task ExecuteCommandAsyncReturnsFailureResultFromGrainAsync()
    {
        // Arrange
        TestCommand command = new("test");
        OperationResult expectedResult = OperationResult.Fail("ERR001", "Something failed");
        Mock<IGenericAggregateGrain<TestAggregate>> mockGrain = new();
        mockGrain.Setup(g => g.ExecuteAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);
        mockFactory.Setup(f => f.GetGenericAggregate<TestAggregate>("entity-1")).Returns(mockGrain.Object);
        TestableAggregateService service = new(mockFactory.Object, NullServiceLogger);

        // Act
        OperationResult result = await service.TestExecuteCommandAsync("entity-1", command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERR001", result.ErrorCode);
        Assert.Equal("Something failed", result.ErrorMessage);
    }

    /// <summary>
    ///     Tests that ExecuteCommandAsync throws when command is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteCommandAsync throws when command is null")]
    public async Task ExecuteCommandAsyncThrowsWhenCommandIsNullAsync()
    {
        // Arrange
        TestableAggregateService service = new(mockFactory.Object, NullServiceLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.TestExecuteCommandAsync<TestCommand>("entity-1", null!));
    }

    /// <summary>
    ///     Tests that ExecuteCommandAsync throws when entityId is empty.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteCommandAsync throws when entityId is empty")]
    public async Task ExecuteCommandAsyncThrowsWhenEntityIdIsEmptyAsync()
    {
        // Arrange
        TestableAggregateService service = new(mockFactory.Object, NullServiceLogger);
        TestCommand command = new("test");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.TestExecuteCommandAsync(string.Empty, command));
    }

    /// <summary>
    ///     Tests that ExecuteCommandAsync throws when entityId is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteCommandAsync throws when entityId is null")]
    public async Task ExecuteCommandAsyncThrowsWhenEntityIdIsNullAsync()
    {
        // Arrange
        TestableAggregateService service = new(mockFactory.Object, NullServiceLogger);
        TestCommand command = new("test");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.TestExecuteCommandAsync(null!, command));
    }

    /// <summary>
    ///     Tests that ExecuteCommandAsync throws when entityId is whitespace.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteCommandAsync throws when entityId is whitespace")]
    public async Task ExecuteCommandAsyncThrowsWhenEntityIdIsWhitespaceAsync()
    {
        // Arrange
        TestableAggregateService service = new(mockFactory.Object, NullServiceLogger);
        TestCommand command = new("test");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.TestExecuteCommandAsync("   ", command));
    }

    /// <summary>
    ///     Tests that Logger property returns injected logger.
    /// </summary>
    [Fact(DisplayName = "Logger property returns injected logger")]
    public void LoggerPropertyReturnsInjectedLogger()
    {
        // Arrange & Act
        TestableAggregateService service = new(mockFactory.Object, NullServiceLogger);

        // Assert
        Assert.Same(NullServiceLogger, service.ExposedLogger);
    }

    /// <summary>
    ///     Tests that OnAfterExecuteAsync is called after grain execution.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "OnAfterExecuteAsync is called after grain execution")]
    public async Task OnAfterExecuteAsyncIsCalledAfterGrainExecutionAsync()
    {
        // Arrange
        TestCommand command = new("test");
        OperationResult expectedResult = OperationResult.Ok();
        Mock<IGenericAggregateGrain<TestAggregate>> mockGrain = new();
        mockGrain.Setup(g => g.ExecuteAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);
        mockFactory.Setup(f => f.GetGenericAggregate<TestAggregate>("entity-1")).Returns(mockGrain.Object);
        TrackingAggregateService service = new(mockFactory.Object, NullServiceLogger);

        // Act
        await service.TestExecuteCommandAsync("entity-1", command);

        // Assert
        Assert.True(service.OnAfterExecuteCalled);
        Assert.Equal("entity-1", service.AfterEntityId);
        Assert.NotNull(service.AfterResult);
        Assert.True(service.AfterResult.Value.Success);
    }

    /// <summary>
    ///     Tests that OnBeforeExecuteAsync is called before grain execution.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "OnBeforeExecuteAsync is called before grain execution")]
    public async Task OnBeforeExecuteAsyncIsCalledBeforeGrainExecutionAsync()
    {
        // Arrange
        TestCommand command = new("test");
        OperationResult expectedResult = OperationResult.Ok();
        Mock<IGenericAggregateGrain<TestAggregate>> mockGrain = new();
        mockGrain.Setup(g => g.ExecuteAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);
        mockFactory.Setup(f => f.GetGenericAggregate<TestAggregate>("entity-1")).Returns(mockGrain.Object);
        TrackingAggregateService service = new(mockFactory.Object, NullServiceLogger);

        // Act
        await service.TestExecuteCommandAsync("entity-1", command);

        // Assert
        Assert.True(service.OnBeforeExecuteCalled);
        Assert.Equal("entity-1", service.BeforeEntityId);
    }
}
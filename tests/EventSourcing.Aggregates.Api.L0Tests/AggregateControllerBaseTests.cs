using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.Api.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateControllerBase{TAggregate}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates API")]
[AllureSubSuite("Controller Base")]
public sealed class AggregateControllerBaseTests
{
    private static ILogger<AggregateControllerBase<TestAggregate>> NullControllerLogger =>
        NullLogger<AggregateControllerBase<TestAggregate>>.Instance;

    private static Task<OperationResult> ServiceMethodAsync(
        string entityId,
        TestCommand cmd,
        CancellationToken ct
    ) =>
        Task.FromResult(OperationResult.Ok());

    /// <summary>
    ///     Controller that short-circuits with Unauthorized.
    /// </summary>
    private sealed class ShortCircuitController : AggregateControllerBase<TestAggregate>
    {
        public ShortCircuitController(
            ILogger<AggregateControllerBase<TestAggregate>> logger
        )
            : base(logger)
        {
        }

        public Task<ActionResult<OperationResult>> TestExecuteAsync<TCommand>(
            string entityId,
            TCommand command,
            Func<string, TCommand, CancellationToken, Task<OperationResult>> serviceMethod,
            CancellationToken cancellationToken = default
        )
            where TCommand : class =>
            ExecuteAsync(entityId, command, serviceMethod, cancellationToken);

        protected override Task<ActionResult?> OnBeforeExecuteAsync<TCommand>(
            string entityId,
            TCommand command,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult<ActionResult?>(new UnauthorizedResult());
    }

    /// <summary>
    ///     Testable implementation of AggregateControllerBase for testing.
    /// </summary>
    private sealed class TestableAggregateController : AggregateControllerBase<TestAggregate>
    {
        public TestableAggregateController(
            ILogger<AggregateControllerBase<TestAggregate>> logger
        )
            : base(logger)
        {
        }

        public ILogger<AggregateControllerBase<TestAggregate>> ExposedLogger => Logger;

        public Task<ActionResult<OperationResult>> TestExecuteAsync<TCommand>(
            string entityId,
            TCommand command,
            Func<string, TCommand, CancellationToken, Task<OperationResult>> serviceMethod,
            CancellationToken cancellationToken = default
        )
            where TCommand : class =>
            ExecuteAsync(entityId, command, serviceMethod, cancellationToken);
    }

    /// <summary>
    ///     Tracking implementation to verify hook methods are called.
    /// </summary>
    private sealed class TrackingAggregateController : AggregateControllerBase<TestAggregate>
    {
        public TrackingAggregateController(
            ILogger<AggregateControllerBase<TestAggregate>> logger
        )
            : base(logger)
        {
        }

        public string? AfterEntityId { get; private set; }

        public OperationResult? AfterResult { get; private set; }

        public string? BeforeEntityId { get; private set; }

        public bool OnAfterExecuteCalled { get; private set; }

        public bool OnBeforeExecuteCalled { get; private set; }

        public Task<ActionResult<OperationResult>> TestExecuteAsync<TCommand>(
            string entityId,
            TCommand command,
            Func<string, TCommand, CancellationToken, Task<OperationResult>> serviceMethod,
            CancellationToken cancellationToken = default
        )
            where TCommand : class =>
            ExecuteAsync(entityId, command, serviceMethod, cancellationToken);

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

        protected override Task<ActionResult?> OnBeforeExecuteAsync<TCommand>(
            string entityId,
            TCommand command,
            CancellationToken cancellationToken = default
        )
        {
            OnBeforeExecuteCalled = true;
            BeforeEntityId = entityId;
            return Task.FromResult<ActionResult?>(null);
        }
    }

    /// <summary>
    ///     Tests that constructor throws when logger is null.
    /// </summary>
    [Fact(DisplayName = "Constructor throws when logger is null")]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableAggregateController(null!));
    }

    /// <summary>
    ///     Tests that ExecuteAsync returns BadRequest when InvalidOperationException is thrown.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteAsync returns BadRequest when InvalidOperationException is thrown")]
    public async Task ExecuteAsyncReturnsBadRequestWhenInvalidOperationExceptionIsThrownAsync()
    {
        // Arrange
        TestableAggregateController controller = new(NullControllerLogger);
        TestCommand command = new("test");

        static Task<OperationResult> ThrowingServiceMethodAsync(
            string id,
            TestCommand cmd,
            CancellationToken ct
        ) =>
            throw new InvalidOperationException("Test exception");

        // Act
        ActionResult<OperationResult> result = await controller.TestExecuteAsync(
            "entity-1",
            command,
            ThrowingServiceMethodAsync);

        // Assert
        BadRequestObjectResult badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        OperationResult value = Assert.IsType<OperationResult>(badResult.Value);
        Assert.False(value.Success);
        Assert.Equal("CommandExecutionFailed", value.ErrorCode);
        Assert.Contains("Test exception", value.ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Tests that ExecuteAsync returns BadRequest when operation fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteAsync returns BadRequest when operation fails")]
    public async Task ExecuteAsyncReturnsBadRequestWhenOperationFailsAsync()
    {
        // Arrange
        TestableAggregateController controller = new(NullControllerLogger);
        TestCommand command = new("test");

        Task<OperationResult> FailingServiceMethodAsync(
            string id,
            TestCommand cmd,
            CancellationToken ct
        ) =>
            Task.FromResult(OperationResult.Fail("ERR001", "Something failed"));

        // Act
        ActionResult<OperationResult> result = await controller.TestExecuteAsync(
            "entity-1",
            command,
            FailingServiceMethodAsync);

        // Assert
        BadRequestObjectResult badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        OperationResult value = Assert.IsType<OperationResult>(badResult.Value);
        Assert.False(value.Success);
        Assert.Equal("ERR001", value.ErrorCode);
    }

    /// <summary>
    ///     Tests that ExecuteAsync returns Ok when operation succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteAsync returns Ok when operation succeeds")]
    public async Task ExecuteAsyncReturnsOkWhenOperationSucceedsAsync()
    {
        // Arrange
        TestableAggregateController controller = new(NullControllerLogger);
        TestCommand command = new("test");

        // Act
        ActionResult<OperationResult> result = await controller.TestExecuteAsync(
            "entity-1",
            command,
            ServiceMethodAsync);

        // Assert
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        OperationResult value = Assert.IsType<OperationResult>(okResult.Value);
        Assert.True(value.Success);
    }

    /// <summary>
    ///     Tests that ExecuteAsync throws when command is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteAsync throws when command is null")]
    public async Task ExecuteAsyncThrowsWhenCommandIsNullAsync()
    {
        // Arrange
        TestableAggregateController controller = new(NullControllerLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            controller.TestExecuteAsync<TestCommand>("entity-1", null!, ServiceMethodAsync));
    }

    /// <summary>
    ///     Tests that ExecuteAsync throws when entityId is empty.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteAsync throws when entityId is empty")]
    public async Task ExecuteAsyncThrowsWhenEntityIdIsEmptyAsync()
    {
        // Arrange
        TestableAggregateController controller = new(NullControllerLogger);
        TestCommand command = new("test");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            controller.TestExecuteAsync(string.Empty, command, ServiceMethodAsync));
    }

    /// <summary>
    ///     Tests that ExecuteAsync throws when entityId is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteAsync throws when entityId is null")]
    public async Task ExecuteAsyncThrowsWhenEntityIdIsNullAsync()
    {
        // Arrange
        TestableAggregateController controller = new(NullControllerLogger);
        TestCommand command = new("test");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            controller.TestExecuteAsync(null!, command, ServiceMethodAsync));
    }

    /// <summary>
    ///     Tests that ExecuteAsync throws when entityId is whitespace.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteAsync throws when entityId is whitespace")]
    public async Task ExecuteAsyncThrowsWhenEntityIdIsWhitespaceAsync()
    {
        // Arrange
        TestableAggregateController controller = new(NullControllerLogger);
        TestCommand command = new("test");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            controller.TestExecuteAsync("   ", command, ServiceMethodAsync));
    }

    /// <summary>
    ///     Tests that ExecuteAsync throws when serviceMethod is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "ExecuteAsync throws when serviceMethod is null")]
    public async Task ExecuteAsyncThrowsWhenServiceMethodIsNullAsync()
    {
        // Arrange
        TestableAggregateController controller = new(NullControllerLogger);
        TestCommand command = new("test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            controller.TestExecuteAsync<TestCommand>("entity-1", command, null!));
    }

    /// <summary>
    ///     Tests that Logger property returns injected logger.
    /// </summary>
    [Fact(DisplayName = "Logger property returns injected logger")]
    public void LoggerPropertyReturnsInjectedLogger()
    {
        // Arrange & Act
        TestableAggregateController controller = new(NullControllerLogger);

        // Assert
        Assert.Same(NullControllerLogger, controller.ExposedLogger);
    }

    /// <summary>
    ///     Tests that OnAfterExecuteAsync is called after successful execution.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "OnAfterExecuteAsync is called after successful execution")]
    public async Task OnAfterExecuteAsyncIsCalledAfterSuccessfulExecutionAsync()
    {
        // Arrange
        TrackingAggregateController controller = new(NullControllerLogger);
        TestCommand command = new("test");

        // Act
        await controller.TestExecuteAsync("entity-1", command, ServiceMethodAsync);

        // Assert
        Assert.True(controller.OnAfterExecuteCalled);
        Assert.Equal("entity-1", controller.AfterEntityId);
        Assert.NotNull(controller.AfterResult);
        ActionResult<OperationResult> afterResult = controller.AfterResult!;
        Assert.True(afterResult.Value.Success);
    }

    /// <summary>
    ///     Tests that OnBeforeExecuteAsync can short-circuit execution.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "OnBeforeExecuteAsync can short-circuit execution")]
    public async Task OnBeforeExecuteAsyncCanShortCircuitExecutionAsync()
    {
        // Arrange
        ShortCircuitController controller = new(NullControllerLogger);
        TestCommand command = new("test");
        bool serviceMethodCalled = false;

        Task<OperationResult> TrackingServiceMethodAsync(
            string id,
            TestCommand cmd,
            CancellationToken ct
        )
        {
            serviceMethodCalled = true;
            return Task.FromResult(OperationResult.Ok());
        }

        // Act
        ActionResult<OperationResult> result = await controller.TestExecuteAsync(
            "entity-1",
            command,
            TrackingServiceMethodAsync);

        // Assert
        Assert.False(serviceMethodCalled);
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    /// <summary>
    ///     Tests that OnBeforeExecuteAsync is called before execution.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact(DisplayName = "OnBeforeExecuteAsync is called before execution")]
    public async Task OnBeforeExecuteAsyncIsCalledBeforeExecutionAsync()
    {
        // Arrange
        TrackingAggregateController controller = new(NullControllerLogger);
        TestCommand command = new("test");

        // Act
        await controller.TestExecuteAsync("entity-1", command, ServiceMethodAsync);

        // Assert
        Assert.True(controller.OnBeforeExecuteCalled);
        Assert.Equal("entity-1", controller.BeforeEntityId);
    }
}
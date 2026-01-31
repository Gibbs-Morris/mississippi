using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="RootCommandHandler{TSnapshot}" />.
/// </summary>
public sealed class RootCommandHandlerTests
{
    private sealed record CreateCommand(string Name);

    /// <summary>
    ///     Command handler that matches CreateCommand.
    /// </summary>
    private sealed class CreateCommandHandler : ICommandHandler<CreateCommand, TestState>
    {
        public int InvocationCount { get; private set; }

        public OperationResult<IReadOnlyList<object>> Handle(
            CreateCommand command,
            TestState? state
        )
        {
            InvocationCount++;
            return OperationResult.Ok<IReadOnlyList<object>>(new[] { new TestEvent() });
        }

        public bool TryHandle(
            object command,
            TestState? state,
            out OperationResult<IReadOnlyList<object>> result
        )
        {
            if (command is CreateCommand typedCommand)
            {
                result = Handle(typedCommand, state);
                return true;
            }

            result = default!;
            return false;
        }
    }

    private sealed record DeleteCommand;

    /// <summary>
    ///     Fallback command handler that does not implement the generic interface.
    ///     Used to test the fallback path.
    /// </summary>
    private sealed class FallbackCommandHandler : ICommandHandler<TestState>
    {
        private readonly Func<object, bool> predicate;

        public FallbackCommandHandler(
            Func<object, bool> predicate
        ) =>
            this.predicate = predicate;

        public int InvocationCount { get; private set; }

        public bool TryHandle(
            object command,
            TestState? state,
            out OperationResult<IReadOnlyList<object>> result
        )
        {
            InvocationCount++;
            if (predicate(command))
            {
                result = OperationResult.Ok<IReadOnlyList<object>>(new[] { new TestEvent() });
                return true;
            }

            result = default!;
            return false;
        }
    }

    /// <summary>
    ///     A second handler for CreateCommand to test first-match-wins with duplicates.
    /// </summary>
    private sealed class SecondCreateCommandHandler : ICommandHandler<CreateCommand, TestState>
    {
        public int InvocationCount { get; private set; }

        public OperationResult<IReadOnlyList<object>> Handle(
            CreateCommand command,
            TestState? state
        )
        {
            InvocationCount++;
            return OperationResult.Ok<IReadOnlyList<object>>(new[] { new TestEvent(), new TestEvent() });
        }

        public bool TryHandle(
            object command,
            TestState? state,
            out OperationResult<IReadOnlyList<object>> result
        )
        {
            if (command is CreateCommand typedCommand)
            {
                result = Handle(typedCommand, state);
                return true;
            }

            result = default!;
            return false;
        }
    }

    private sealed class TestEvent;

    private sealed record TestState(int Count);

    private sealed record UnhandledCommand;

    private sealed record UpdateCommand(string Value);

    /// <summary>
    ///     Command handler that matches UpdateCommand.
    /// </summary>
    private sealed class UpdateCommandHandler : ICommandHandler<UpdateCommand, TestState>
    {
        public int InvocationCount { get; private set; }

        public OperationResult<IReadOnlyList<object>> Handle(
            UpdateCommand command,
            TestState? state
        )
        {
            InvocationCount++;
            return OperationResult.Ok<IReadOnlyList<object>>(new[] { new TestEvent() });
        }

        public bool TryHandle(
            object command,
            TestState? state,
            out OperationResult<IReadOnlyList<object>> result
        )
        {
            if (command is UpdateCommand typedCommand)
            {
                result = Handle(typedCommand, state);
                return true;
            }

            result = default!;
            return false;
        }
    }

    /// <summary>
    ///     Constructor should throw when handlers is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenHandlersIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RootCommandHandler<TestState>(null!));
    }

    /// <summary>
    ///     Handle should check indexed handlers before fallback.
    /// </summary>
    [Fact]
    public void HandleChecksIndexedHandlersBeforeFallback()
    {
        CreateCommandHandler createHandler = new();
        FallbackCommandHandler fallbackHandler = new(_ => true);
        RootCommandHandler<TestState>
            handler = new(new ICommandHandler<TestState>[] { createHandler, fallbackHandler });
        OperationResult<IReadOnlyList<object>> result = handler.Handle(new CreateCommand("test"), null);
        Assert.True(result.Success);
        Assert.Equal(1, createHandler.InvocationCount);

        // Fallback should not be invoked when indexed handler matches
        Assert.Equal(0, fallbackHandler.InvocationCount);
    }

    /// <summary>
    ///     Handle should dispatch to the correct handler via type index.
    /// </summary>
    [Fact]
    public void HandleDispatchesToCorrectHandlerViaTypeIndex()
    {
        CreateCommandHandler createHandler = new();
        UpdateCommandHandler updateHandler = new();
        RootCommandHandler<TestState> handler = new(new ICommandHandler<TestState>[] { createHandler, updateHandler });
        OperationResult<IReadOnlyList<object>> result = handler.Handle(new UpdateCommand("test"), null);
        Assert.True(result.Success);
        Assert.Equal(0, createHandler.InvocationCount);
        Assert.Equal(1, updateHandler.InvocationCount);
    }

    /// <summary>
    ///     Handle should not invoke handlers for other command types.
    /// </summary>
    [Fact]
    public void HandleDoesNotInvokeHandlersForOtherCommandTypes()
    {
        CreateCommandHandler createHandler = new();
        UpdateCommandHandler updateHandler = new();
        RootCommandHandler<TestState> handler = new(new ICommandHandler<TestState>[] { createHandler, updateHandler });
        handler.Handle(new CreateCommand("test"), null);
        Assert.Equal(1, createHandler.InvocationCount);
        Assert.Equal(0, updateHandler.InvocationCount);
    }

    /// <summary>
    ///     Handle should fall through to fallback when indexed handler does not match.
    /// </summary>
    [Fact]
    public void HandleFallsToFallbackWhenIndexedHandlerDoesNotMatch()
    {
        CreateCommandHandler createHandler = new();
        FallbackCommandHandler fallbackHandler = new(cmd => cmd is DeleteCommand);
        RootCommandHandler<TestState>
            handler = new(new ICommandHandler<TestState>[] { createHandler, fallbackHandler });
        OperationResult<IReadOnlyList<object>> result = handler.Handle(new DeleteCommand(), null);
        Assert.True(result.Success);
        Assert.Equal(0, createHandler.InvocationCount);
        Assert.Equal(1, fallbackHandler.InvocationCount);
    }

    /// <summary>
    ///     Handle should pass state to the matched handler.
    /// </summary>
    [Fact]
    public void HandlePassesStateToMatchedHandler()
    {
        TestState? capturedState = null;
        DelegateCommandHandler<CreateCommand, TestState> createHandler = new((
            _,
            state
        ) =>
        {
            capturedState = state;
            return OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>());
        });
        TestState expectedState = new(42);
        RootCommandHandler<TestState> handler = new(new ICommandHandler<TestState>[] { createHandler });
        handler.Handle(new CreateCommand("test"), expectedState);
        Assert.Same(expectedState, capturedState);
    }

    /// <summary>
    ///     Handle should preserve first-match-wins ordering when multiple handlers match same type.
    /// </summary>
    [Fact]
    public void HandlePreservesFirstMatchWinsOrdering()
    {
        CreateCommandHandler firstHandler = new();
        SecondCreateCommandHandler secondHandler = new();
        RootCommandHandler<TestState> handler = new(new ICommandHandler<TestState>[] { firstHandler, secondHandler });
        OperationResult<IReadOnlyList<object>> result = handler.Handle(new CreateCommand("test"), null);
        Assert.True(result.Success);
        Assert.Single(result.Value!);
        Assert.Equal(1, firstHandler.InvocationCount);
        Assert.Equal(0, secondHandler.InvocationCount);
    }

    /// <summary>
    ///     Handle should return failure when no handler matches.
    /// </summary>
    [Fact]
    public void HandleReturnsFailureWhenNoHandlerMatches()
    {
        CreateCommandHandler createHandler = new();
        RootCommandHandler<TestState> handler = new(new ICommandHandler<TestState>[] { createHandler });
        OperationResult<IReadOnlyList<object>> result = handler.Handle(new UnhandledCommand(), null);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.CommandHandlerNotFound, result.ErrorCode);
        Assert.Equal(0, createHandler.InvocationCount);
    }

    /// <summary>
    ///     Handle should throw when command is null.
    /// </summary>
    [Fact]
    public void HandleThrowsWhenCommandIsNull()
    {
        RootCommandHandler<TestState> handler = new(Array.Empty<ICommandHandler<TestState>>());
        Assert.Throws<ArgumentNullException>(() => handler.Handle(null!, null));
    }

    /// <summary>
    ///     Handle should use fallback path for handlers without generic interface.
    /// </summary>
    [Fact]
    public void HandleUsesFallbackPathForNonGenericHandlers()
    {
        FallbackCommandHandler fallbackHandler = new(cmd => cmd is DeleteCommand);
        RootCommandHandler<TestState> handler = new(new ICommandHandler<TestState>[] { fallbackHandler });
        OperationResult<IReadOnlyList<object>> result = handler.Handle(new DeleteCommand(), null);
        Assert.True(result.Success);
        Assert.Equal(1, fallbackHandler.InvocationCount);
    }

    /// <summary>
    ///     Handle should work with empty handlers collection.
    /// </summary>
    [Fact]
    public void HandleWorksWithEmptyHandlersCollection()
    {
        RootCommandHandler<TestState> handler = new(Array.Empty<ICommandHandler<TestState>>());
        OperationResult<IReadOnlyList<object>> result = handler.Handle(new CreateCommand("test"), null);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.CommandHandlerNotFound, result.ErrorCode);
    }
}
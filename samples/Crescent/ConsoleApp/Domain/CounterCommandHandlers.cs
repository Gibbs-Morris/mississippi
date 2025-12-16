using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Command handler for initializing a counter.
/// </summary>
public sealed class InitializeCounterHandler : ICommandHandler<InitializeCounter, CounterState>
{
    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        InitializeCounter command,
        CounterState? state
    )
    {
        // Prevent re-initialization
        if (state?.IsInitialized == true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.AlreadyExists,
                "Counter is already initialized.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new CounterInitialized
                {
                    InitialValue = command.InitialValue,
                },
            });
    }
}

/// <summary>
///     Command handler for incrementing a counter.
/// </summary>
public sealed class IncrementCounterHandler : ICommandHandler<IncrementCounter, CounterState>
{
    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        IncrementCounter command,
        CounterState? state
    )
    {
        if (state?.IsInitialized != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Counter must be initialized before incrementing.");
        }

        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Increment amount must be positive.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new CounterIncremented
                {
                    Amount = command.Amount,
                },
            });
    }
}

/// <summary>
///     Command handler for decrementing a counter.
/// </summary>
public sealed class DecrementCounterHandler : ICommandHandler<DecrementCounter, CounterState>
{
    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        DecrementCounter command,
        CounterState? state
    )
    {
        if (state?.IsInitialized != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Counter must be initialized before decrementing.");
        }

        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Decrement amount must be positive.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new CounterDecremented
                {
                    Amount = command.Amount,
                },
            });
    }
}

/// <summary>
///     Command handler for resetting a counter.
/// </summary>
public sealed class ResetCounterHandler : ICommandHandler<ResetCounter, CounterState>
{
    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        ResetCounter command,
        CounterState? state
    )
    {
        if (state?.IsInitialized != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Counter must be initialized before resetting.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new CounterReset
                {
                    NewValue = command.NewValue,
                    PreviousValue = state.Count,
                },
            });
    }
}
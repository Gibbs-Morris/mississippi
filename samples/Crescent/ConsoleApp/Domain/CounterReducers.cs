using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Reducer for <see cref="CounterInitialized" /> events.
/// </summary>
public sealed class CounterInitializedReducer : Reducer<CounterInitialized, CounterState>
{
    /// <inheritdoc />
    protected override CounterState ReduceCore(
        CounterState state,
        CounterInitialized @event
    ) =>
        (state ?? new()) with
        {
            Count = @event.InitialValue,
            IsInitialized = true,
        };
}

/// <summary>
///     Reducer for <see cref="CounterIncremented" /> events.
/// </summary>
public sealed class CounterIncrementedReducer : Reducer<CounterIncremented, CounterState>
{
    /// <inheritdoc />
    protected override CounterState ReduceCore(
        CounterState state,
        CounterIncremented @event
    ) =>
        (state ?? new()) with
        {
            Count = state?.Count + @event.Amount ?? @event.Amount,
            IncrementCount = (state?.IncrementCount ?? 0) + 1,
        };
}

/// <summary>
///     Reducer for <see cref="CounterDecremented" /> events.
/// </summary>
public sealed class CounterDecrementedReducer : Reducer<CounterDecremented, CounterState>
{
    /// <inheritdoc />
    protected override CounterState ReduceCore(
        CounterState state,
        CounterDecremented @event
    ) =>
        (state ?? new()) with
        {
            Count = state?.Count - @event.Amount ?? -@event.Amount,
            DecrementCount = (state?.DecrementCount ?? 0) + 1,
        };
}

/// <summary>
///     Reducer for <see cref="CounterReset" /> events.
/// </summary>
public sealed class CounterResetReducer : Reducer<CounterReset, CounterState>
{
    /// <inheritdoc />
    protected override CounterState ReduceCore(
        CounterState state,
        CounterReset @event
    ) =>
        (state ?? new()) with
        {
            Count = @event.NewValue,
            ResetCount = (state?.ResetCount ?? 0) + 1,
        };
}
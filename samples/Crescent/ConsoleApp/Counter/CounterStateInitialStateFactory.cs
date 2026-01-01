using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Crescent.ConsoleApp.Counter;

/// <summary>
///     Factory for creating the initial state of <see cref="CounterState" /> snapshots.
/// </summary>
internal sealed class CounterStateInitialStateFactory : IInitialStateFactory<CounterState>
{
    /// <inheritdoc />
    public CounterState Create() =>
        new()
        {
            IsInitialized = false,
            Count = 0,
            IncrementCount = 0,
            DecrementCount = 0,
            ResetCount = 0,
        };
}
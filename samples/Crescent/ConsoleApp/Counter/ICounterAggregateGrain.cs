using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Orleans;


namespace Crescent.ConsoleApp.Counter;

/// <summary>
///     Grain interface for the counter aggregate.
///     Exposes domain operations for managing a counter.
/// </summary>
[Alias("Crescent.ConsoleApp.Counter.ICounterAggregateGrain")]
internal interface ICounterAggregateGrain : IAggregateGrain
{
    /// <summary>
    ///     Decrements the counter by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to decrement. Defaults to 1.</param>
    /// <returns>The operation result.</returns>
    [Alias("Decrement")]
    Task<OperationResult> DecrementAsync(
        int amount = 1
    );

    /// <summary>
    ///     Increments the counter by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to increment. Defaults to 1.</param>
    /// <returns>The operation result.</returns>
    [Alias("Increment")]
    Task<OperationResult> IncrementAsync(
        int amount = 1
    );

    /// <summary>
    ///     Initializes the counter with the specified value.
    /// </summary>
    /// <param name="initialValue">The initial value for the counter.</param>
    /// <returns>The operation result.</returns>
    [Alias("Initialize")]
    Task<OperationResult> InitializeAsync(
        int initialValue = 0
    );

    /// <summary>
    ///     Resets the counter to the specified value.
    /// </summary>
    /// <param name="newValue">The value to reset to. Defaults to 0.</param>
    /// <returns>The operation result.</returns>
    [Alias("Reset")]
    Task<OperationResult> ResetAsync(
        int newValue = 0
    );
}

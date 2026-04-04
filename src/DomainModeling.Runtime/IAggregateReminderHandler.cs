using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;

using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Handles reminder callbacks for aggregate grains that opt into reminder-driven behavior.
/// </summary>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
internal interface IAggregateReminderHandler<TAggregate>
    where TAggregate : class
{
    /// <summary>
    ///     Processes a reminder callback for the specified aggregate instance.
    /// </summary>
    /// <param name="entityId">The aggregate entity identifier.</param>
    /// <param name="reminderName">The Orleans reminder name.</param>
    /// <param name="status">The current reminder tick status.</param>
    /// <param name="loadStateAsync">Delegate for loading the current aggregate state.</param>
    /// <param name="executeCommandAsync">Delegate for executing a runtime command against the aggregate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     <c>true</c> when the reminder name is recognized by this handler; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> ReceiveReminderAsync(
        string entityId,
        string reminderName,
        TickStatus status,
        Func<CancellationToken, Task<TAggregate?>> loadStateAsync,
        Func<object, CancellationToken, Task<OperationResult>> executeCommandAsync,
        CancellationToken cancellationToken = default
    );
}
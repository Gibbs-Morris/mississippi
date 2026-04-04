using System;
using System.Threading;
using System.Threading.Tasks;

using Orleans;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Reconciles Orleans reminder state for aggregate types that participate in framework-owned reminders.
/// </summary>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
internal interface IAggregateReminderReconciler<TAggregate>
    where TAggregate : class
{
    /// <summary>
    ///     Reconciles Orleans reminder registration with the aggregate's current persisted state.
    /// </summary>
    /// <param name="grain">The active aggregate grain instance.</param>
    /// <param name="entityId">The aggregate entity identifier.</param>
    /// <param name="loadStateAsync">Delegate used to load the latest aggregate state.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReconcileAsync(
        IGrainBase grain,
        string entityId,
        Func<CancellationToken, Task<TAggregate?>> loadStateAsync,
        CancellationToken cancellationToken = default
    );
}
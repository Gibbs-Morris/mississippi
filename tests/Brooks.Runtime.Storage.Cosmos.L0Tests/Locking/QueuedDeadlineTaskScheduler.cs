using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;

/// <summary>
///     Retains real deadline tasks until a test explicitly permits their execution.
/// </summary>
internal sealed class QueuedDeadlineTaskScheduler : TaskScheduler
{
    private readonly ConcurrentQueue<Task> pending = new();

    /// <summary>
    ///     Gets the number of actual tasks whose execution is still held.
    /// </summary>
    public int PendingTasks => pending.Count;

    /// <summary>
    ///     Gets the barrier proving that an actual dispatched task reached its scheduler.
    /// </summary>
    public TaskCompletionSource Queued { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Executes the next retained task without blocking on a fixture barrier.
    /// </summary>
    public void ExecuteNext()
    {
        if (!pending.TryDequeue(out Task? task) || !TryExecuteTask(task))
        {
            throw new InvalidOperationException("No queued deadline task could be executed.");
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<Task> GetScheduledTasks() => pending.ToArray();

    /// <inheritdoc />
    protected override void QueueTask(
        Task task
    )
    {
        pending.Enqueue(task);
        Queued.TrySetResult();
    }

    /// <inheritdoc />
    protected override bool TryExecuteTaskInline(
        Task task,
        bool taskWasPreviouslyQueued
    ) =>
        false;
}
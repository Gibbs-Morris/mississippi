using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Common.Runtime.Storage.Abstractions.Retry;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Executes a test operation once without retry behavior.
/// </summary>
internal sealed class PassThroughRetryPolicy : IRetryPolicy
{
    /// <summary>
    ///     Executes the supplied operation once.
    /// </summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The operation result.</returns>
    public Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default
    ) =>
        operation();
}
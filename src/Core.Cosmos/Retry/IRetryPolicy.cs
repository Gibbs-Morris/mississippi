using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Core.Cosmos.Retry;

/// <summary>
///     Provides retry functionality for operations that may fail due to transient errors.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    ///     Executes the specified operation with retry logic for handling transient failures.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute with retry logic.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The result of the operation if successful after retries.</returns>
    Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default
    );
}
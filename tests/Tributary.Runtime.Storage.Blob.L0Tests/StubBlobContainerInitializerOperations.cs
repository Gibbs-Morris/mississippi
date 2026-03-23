using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Tributary.Runtime.Storage.Blob.Startup;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Test double for Blob container startup operations.
/// </summary>
internal sealed class StubBlobContainerInitializerOperations : IBlobContainerInitializerOperations
{
    /// <summary>
    ///     Gets the number of times <see cref="CreateIfNotExistsAsync" /> was invoked.
    /// </summary>
    public int CreateIfNotExistsCallCount { get; private set; }

    /// <summary>
    ///     Gets the exception thrown by <see cref="CreateIfNotExistsAsync" /> when configured.
    /// </summary>
    public Exception? CreateIfNotExistsException { get; init; }

    /// <summary>
    ///     Gets the number of times <see cref="ExistsAsync" /> was invoked.
    /// </summary>
    public int ExistsCallCount { get; private set; }

    /// <summary>
    ///     Gets the exception thrown by <see cref="ExistsAsync" /> when configured.
    /// </summary>
    public Exception? ExistsException { get; init; }

    /// <summary>
    ///     Gets a value indicating whether <see cref="ExistsAsync" /> returns <see langword="true" />.
    /// </summary>
    public bool ExistsResult { get; init; }

    /// <inheritdoc />
    public Task CreateIfNotExistsAsync(
        CancellationToken cancellationToken
    )
    {
        CreateIfNotExistsCallCount++;
        if (CreateIfNotExistsException is not null)
        {
            throw CreateIfNotExistsException;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(
        CancellationToken cancellationToken
    )
    {
        ExistsCallCount++;
        if (ExistsException is not null)
        {
            throw ExistsException;
        }

        return Task.FromResult(ExistsResult);
    }
}
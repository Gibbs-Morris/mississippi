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
    ///     Gets a value indicating whether <see cref="ExistsAsync" /> returns <see langword="true" />.
    /// </summary>
    public bool ExistsResult { get; init; }

    /// <summary>
    ///     Gets the number of times <see cref="ExistsAsync" /> was invoked.
    /// </summary>
    public int ExistsCallCount { get; private set; }

    /// <inheritdoc />
    public Task CreateIfNotExistsAsync(
        CancellationToken cancellationToken
    )
    {
        CreateIfNotExistsCallCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(
        CancellationToken cancellationToken
    )
    {
        ExistsCallCount++;
        return Task.FromResult(ExistsResult);
    }
}
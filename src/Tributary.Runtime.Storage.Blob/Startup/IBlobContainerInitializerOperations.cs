using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Startup;

/// <summary>
///     Provides the Blob container operations needed during startup validation.
/// </summary>
internal interface IBlobContainerInitializerOperations
{
    /// <summary>
    ///     Creates the configured container when it does not already exist.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateIfNotExistsAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Determines whether the configured container exists.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns><c>true</c> when the container exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsAsync(CancellationToken cancellationToken);
}
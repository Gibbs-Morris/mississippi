using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos;

/// <summary>
///     Provides functionality for recovering and managing brook cursor positions in Cosmos DB.
/// </summary>
internal interface IBrookRecoveryService
{
    /// <summary>
    ///     Gets the current cursor position for a brook, or recovers it if necessary.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The current or recovered cursor position of the brook.</returns>
    Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Gets the current cursor position for a brook, or recovers it if necessary.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="beforeDispatch">The append ownership check, or null for an ordinary standalone call.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The current or recovered cursor position of the brook.</returns>
    Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        Action? beforeDispatch,
        CancellationToken cancellationToken
    );
}
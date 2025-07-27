using Mississippi.Core.Abstractions.Brooks;


namespace Mississippi.EventSourcing.Cosmos.Abstractions;

/// <summary>
///     Provides functionality for recovering and managing brook head positions in Cosmos DB.
/// </summary>
internal interface IBrookRecoveryService
{
    /// <summary>
    ///     Gets the current head position for a brook, or recovers it if necessary.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The current or recovered head position of the brook.</returns>
    Task<BrookPosition> GetOrRecoverHeadPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );
}
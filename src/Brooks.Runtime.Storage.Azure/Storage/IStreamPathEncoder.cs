using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Storage;

/// <summary>
///     Encodes deterministic Azure Blob paths for Brooks event-storage artifacts.
/// </summary>
internal interface IStreamPathEncoder
{
    /// <summary>
    ///     Gets the committed cursor path for the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <returns>The deterministic cursor blob path.</returns>
    string GetCursorPath(
        BrookKey brookId
    );

    /// <summary>
    ///     Gets the event blob path for the specified brook and position.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The event position.</param>
    /// <returns>The deterministic event blob path.</returns>
    string GetEventPath(
        BrookKey brookId,
        long position
    );

    /// <summary>
    ///     Gets the distributed lock blob path for the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <returns>The deterministic lock blob path.</returns>
    string GetLockPath(
        BrookKey brookId
    );

    /// <summary>
    ///     Gets the pending-write path for the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <returns>The deterministic pending-write blob path.</returns>
    string GetPendingPath(
        BrookKey brookId
    );
}
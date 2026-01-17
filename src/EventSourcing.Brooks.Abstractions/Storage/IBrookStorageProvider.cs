namespace Mississippi.EventSourcing.Brooks.Abstractions.Storage;

/// <summary>
///     Provides unified read and write access to brook event storage.
/// </summary>
/// <remarks>
///     <para>
///         Implementations of this interface encapsulate the storage backend for event brooks,
///         combining read and write operations from <see cref="IBrookStorageReader" /> and
///         <see cref="IBrookStorageWriter" />.
///     </para>
///     <para>
///         Each provider is identified by a unique <see cref="Format" /> string, enabling
///         the system to route operations to the appropriate storage implementation.
///     </para>
/// </remarks>
public interface IBrookStorageProvider
    : IBrookStorageReader,
      IBrookStorageWriter
{
    /// <summary>
    ///     Gets the canonical format identifier for this brook storage provider (e.g., "cosmos", "sql").
    /// </summary>
    /// <value>A string representing the storage format name.</value>
    string Format { get; }
}
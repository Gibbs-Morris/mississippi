using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Default projection source-state accessor built on the existing UX projection grain seams.
/// </summary>
internal sealed class ReplicaSinkUxProjectionSourceStateAccessor : IReplicaSinkSourceStateAccessor
{
    private delegate ValueTask<ReplicaSinkSourceState> ReaderDelegate(
        IUxProjectionGrainFactory grainFactory,
        string entityId,
        long sourcePosition,
        CancellationToken cancellationToken
    );

    private static readonly ConcurrentDictionary<Type, ReaderDelegate> Readers = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkUxProjectionSourceStateAccessor" /> class.
    /// </summary>
    /// <param name="uxProjectionGrainFactory">The UX projection grain factory.</param>
    public ReplicaSinkUxProjectionSourceStateAccessor(
        IUxProjectionGrainFactory uxProjectionGrainFactory
    ) =>
        UxProjectionGrainFactory = uxProjectionGrainFactory ??
                                   throw new ArgumentNullException(nameof(uxProjectionGrainFactory));

    /// <summary>
    ///     Gets the UX projection grain factory.
    /// </summary>
    private IUxProjectionGrainFactory UxProjectionGrainFactory { get; }

    private static ReaderDelegate CreateReader(
        Type projectionType
    )
    {
        Type readerFactoryType = typeof(ReaderFactory<>).MakeGenericType(projectionType);
        IReaderFactory readerFactory = (IReaderFactory)Activator.CreateInstance(readerFactoryType)!;
        return readerFactory.Create();
    }

    private static async ValueTask<ReplicaSinkSourceState> ReadProjectionStateAsync<TProjection>(
        IUxProjectionGrainFactory grainFactory,
        string entityId,
        long sourcePosition,
        CancellationToken cancellationToken
    )
        where TProjection : class
    {
        IUxProjectionGrain<TProjection> projectionGrain = grainFactory.GetUxProjectionGrain<TProjection>(entityId);
        BrookPosition latestVersion = await projectionGrain.GetLatestVersionAsync(cancellationToken);
        if (latestVersion.NotSet || (latestVersion.Value < sourcePosition))
        {
            throw new ReplicaSinkSourceStateUnavailableException(
                typeof(TProjection),
                entityId,
                sourcePosition,
                latestVersion.NotSet ? null : latestVersion.Value);
        }

        TProjection? projection = await projectionGrain.GetAtVersionAsync(new(sourcePosition), cancellationToken);
        return projection is null
            ? ReplicaSinkSourceState.Deleted(sourcePosition)
            : ReplicaSinkSourceState.FromValue(sourcePosition, projection);
    }

    /// <inheritdoc />
    public ValueTask<ReplicaSinkSourceState> ReadAsync(
        Type projectionType,
        string entityId,
        long sourcePosition,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentOutOfRangeException.ThrowIfNegative(sourcePosition);
        ReaderDelegate reader = Readers.GetOrAdd(projectionType, CreateReader);
        return reader(UxProjectionGrainFactory, entityId, sourcePosition, cancellationToken);
    }

    /// <summary>
    ///     Creates typed source-state readers for cached projection types.
    /// </summary>
    private interface IReaderFactory
    {
        /// <summary>
        ///     Creates the typed source-state reader delegate.
        /// </summary>
        /// <returns>The typed source-state reader delegate.</returns>
        ReaderDelegate Create();
    }

    private sealed class ReaderFactory<TProjection> : IReaderFactory
        where TProjection : class
    {
        /// <inheritdoc />
        public ReaderDelegate Create() => ReadProjectionStateAsync<TProjection>;
    }
}
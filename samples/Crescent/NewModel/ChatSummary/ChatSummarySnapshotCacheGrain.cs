using System.Collections.Immutable;

using Crescent.NewModel.Chat;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans.Runtime;


namespace Crescent.NewModel.ChatSummary;

/// <summary>
///     Snapshot cache grain for <see cref="ChatSummaryProjection" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This grain provides versioned, immutable access to projection snapshots.
///         It is used by <see cref="ChatSummaryProjectionGrain" /> to fetch
///         cached projections for UX display purposes.
///     </para>
///     <para>
///         The grain uses retention-based loading to efficiently rebuild state
///         from the nearest retained base snapshot plus a minimal delta of events.
///     </para>
/// </remarks>
internal sealed class ChatSummarySnapshotCacheGrain : SnapshotCacheGrainBase<ChatSummaryProjection, ChatBrook>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatSummarySnapshotCacheGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="snapshotStorageReader">Reader for loading snapshots from storage.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="rootReducer">The root reducer for computing state from events.</param>
    /// <param name="snapshotStateConverter">Converter for serializing/deserializing state.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="retentionOptions">Options controlling snapshot retention and replay strategy.</param>
    /// <param name="brookEventConverter">Converter for deserializing brook events to domain events.</param>
    /// <param name="logger">Logger instance.</param>
    public ChatSummarySnapshotCacheGrain(
        IGrainContext grainContext,
        ISnapshotStorageReader snapshotStorageReader,
        IBrookGrainFactory brookGrainFactory,
        IRootReducer<ChatSummaryProjection> rootReducer,
        ISnapshotStateConverter<ChatSummaryProjection> snapshotStateConverter,
        ISnapshotGrainFactory snapshotGrainFactory,
        IOptions<SnapshotRetentionOptions> retentionOptions,
        IBrookEventConverter brookEventConverter,
        ILogger<ChatSummarySnapshotCacheGrain> logger
    )
        : base(
            grainContext,
            snapshotStorageReader,
            brookGrainFactory,
            rootReducer,
            snapshotStateConverter,
            snapshotGrainFactory,
            retentionOptions,
            logger) =>
        BrookEventConverter = brookEventConverter;

    private IBrookEventConverter BrookEventConverter { get; }

    /// <inheritdoc />
    protected override ChatSummaryProjection CreateInitialState() =>
        new(
            Name: string.Empty,
            MessageCount: 0,
            LastMessageAt: null,
            LastMessagePreview: null,
            ImmutableHashSet<string>.Empty);

    /// <inheritdoc />
    protected override object DeserializeEvent(
        BrookEvent brookEvent
    ) =>
        BrookEventConverter.ToDomainEvent(brookEvent);
}

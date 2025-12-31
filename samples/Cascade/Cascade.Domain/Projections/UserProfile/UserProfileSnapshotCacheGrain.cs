// <copyright file="UserProfileSnapshotCacheGrain.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Domain.User;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans.Runtime;


namespace Cascade.Domain.Projections.UserProfile;

/// <summary>
///     Snapshot cache grain for <see cref="UserProfileProjection" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This grain provides versioned, immutable access to projection snapshots.
///         It is used by <see cref="UserProfileProjectionGrain" /> to fetch
///         cached projections for UX display purposes.
///     </para>
///     <para>
///         The grain uses retention-based loading to efficiently rebuild state
///         from the nearest retained base snapshot plus a minimal delta of events.
///     </para>
/// </remarks>
internal sealed class UserProfileSnapshotCacheGrain : SnapshotCacheGrainBase<UserProfileProjection, UserBrook>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UserProfileSnapshotCacheGrain" /> class.
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
    public UserProfileSnapshotCacheGrain(
        IGrainContext grainContext,
        ISnapshotStorageReader snapshotStorageReader,
        IBrookGrainFactory brookGrainFactory,
        IRootReducer<UserProfileProjection> rootReducer,
        ISnapshotStateConverter<UserProfileProjection> snapshotStateConverter,
        ISnapshotGrainFactory snapshotGrainFactory,
        IOptions<SnapshotRetentionOptions> retentionOptions,
        IBrookEventConverter brookEventConverter,
        ILogger<UserProfileSnapshotCacheGrain> logger
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
    protected override UserProfileProjection CreateInitialState() =>
        new()
        {
            UserId = string.Empty,
            DisplayName = string.Empty,
            IsOnline = false,
            ChannelCount = 0,
        };

    /// <inheritdoc />
    protected override object DeserializeEvent(
        BrookEvent brookEvent
    ) =>
        BrookEventConverter.ToDomainEvent(brookEvent);
}

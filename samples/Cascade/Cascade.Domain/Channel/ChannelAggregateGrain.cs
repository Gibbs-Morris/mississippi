// <copyright file="ChannelAggregateGrain.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Threading.Tasks;

using Cascade.Domain.Channel.Commands;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans.Runtime;


namespace Cascade.Domain.Channel;

/// <summary>
///     Aggregate grain implementation for the channel domain.
/// </summary>
[BrookName("CASCADE", "CHAT", "CHANNEL")]
internal sealed class ChannelAggregateGrain
    : AggregateGrainBase<ChannelAggregate>,
      IChannelAggregateGrain
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelAggregateGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="brookEventConverter">Converter for domain events to/from brook events.</param>
    /// <param name="rootCommandHandler">The root command handler for processing commands.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="rootReducer">The root reducer for obtaining the reducers hash.</param>
    /// <param name="logger">Logger instance.</param>
    public ChannelAggregateGrain(
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory,
        IBrookEventConverter brookEventConverter,
        IRootCommandHandler<ChannelAggregate> rootCommandHandler,
        ISnapshotGrainFactory snapshotGrainFactory,
        IRootReducer<ChannelAggregate> rootReducer,
        ILogger<ChannelAggregateGrain> logger
    )
        : base(
            grainContext,
            brookGrainFactory,
            brookEventConverter,
            rootCommandHandler,
            snapshotGrainFactory,
            rootReducer.GetReducerHash(),
            logger)
    {
    }

    /// <inheritdoc />
    public Task<OperationResult> AddMemberAsync(
        string userId
    ) =>
        ExecuteAsync(
            new AddMember
            {
                UserId = userId,
            });

    /// <inheritdoc />
    public Task<OperationResult> ArchiveAsync(
        string archivedBy
    ) =>
        ExecuteAsync(
            new ArchiveChannel
            {
                ArchivedBy = archivedBy,
            });

    /// <inheritdoc />
    public Task<OperationResult> CreateAsync(
        string channelId,
        string name,
        string createdBy
    ) =>
        ExecuteAsync(
            new CreateChannel
            {
                ChannelId = channelId,
                Name = name,
                CreatedBy = createdBy,
            });

    /// <inheritdoc />
    public Task<OperationResult> RemoveMemberAsync(
        string userId
    ) =>
        ExecuteAsync(
            new RemoveMember
            {
                UserId = userId,
            });

    /// <inheritdoc />
    public Task<OperationResult> RenameAsync(
        string newName
    ) =>
        ExecuteAsync(
            new RenameChannel
            {
                NewName = newName,
            });
}
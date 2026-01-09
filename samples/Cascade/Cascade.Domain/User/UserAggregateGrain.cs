using System.Threading.Tasks;

using Cascade.Domain.User.Commands;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Brooks.Abstractions.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans.Runtime;

#pragma warning disable CS0618 // Type or member is obsolete - migrating to GenericAggregateGrain pattern

namespace Cascade.Domain.User;

/// <summary>
///     Aggregate grain implementation for the user domain.
/// </summary>
[BrookName("CASCADE", "CHAT", "USER")]
internal sealed class UserAggregateGrain
    : AggregateGrainBase<UserAggregate>,
      IUserAggregateGrain
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UserAggregateGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="brookEventConverter">Converter for domain events to/from brook events.</param>
    /// <param name="rootCommandHandler">The root command handler for processing commands.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="rootReducer">The root reducer for obtaining the reducers hash.</param>
    /// <param name="logger">Logger instance.</param>
    public UserAggregateGrain(
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory,
        IBrookEventConverter brookEventConverter,
        IRootCommandHandler<UserAggregate> rootCommandHandler,
        ISnapshotGrainFactory snapshotGrainFactory,
        IRootReducer<UserAggregate> rootReducer,
        ILogger<UserAggregateGrain> logger
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
    public Task<OperationResult> JoinChannelAsync(
        string channelId
    ) =>
        ExecuteAsync(
            new JoinChannel
            {
                ChannelId = channelId,
            });

    /// <inheritdoc />
    public Task<OperationResult> LeaveChannelAsync(
        string channelId
    ) =>
        ExecuteAsync(
            new LeaveChannel
            {
                ChannelId = channelId,
            });

    /// <inheritdoc />
    public Task<OperationResult> RegisterAsync(
        string userId,
        string displayName
    ) =>
        ExecuteAsync(
            new RegisterUser
            {
                UserId = userId,
                DisplayName = displayName,
            });

    /// <inheritdoc />
    public Task<OperationResult> SetOnlineStatusAsync(
        bool isOnline
    ) =>
        ExecuteAsync(
            new SetOnlineStatus
            {
                IsOnline = isOnline,
            });

    /// <inheritdoc />
    public Task<OperationResult> UpdateDisplayNameAsync(
        string newDisplayName
    ) =>
        ExecuteAsync(
            new UpdateDisplayName
            {
                NewDisplayName = newDisplayName,
            });
}
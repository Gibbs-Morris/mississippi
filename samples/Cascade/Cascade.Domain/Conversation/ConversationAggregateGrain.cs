using System.Threading.Tasks;

using Cascade.Domain.Conversation.Commands;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Brooks.Abstractions.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans.Runtime;

#pragma warning disable CS0618 // Type or member is obsolete - migrating to GenericAggregateGrain pattern

namespace Cascade.Domain.Conversation;

/// <summary>
///     Aggregate grain implementation for the conversation domain.
/// </summary>
[BrookName("CASCADE", "CHAT", "CONVERSATION")]
internal sealed class ConversationAggregateGrain
    : AggregateGrainBase<ConversationAggregate>,
      IConversationAggregateGrain
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConversationAggregateGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="brookEventConverter">Converter for domain events to/from brook events.</param>
    /// <param name="rootCommandHandler">The root command handler for processing commands.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="rootReducer">The root reducer for obtaining the reducers hash.</param>
    /// <param name="logger">Logger instance.</param>
    public ConversationAggregateGrain(
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory,
        IBrookEventConverter brookEventConverter,
        IRootCommandHandler<ConversationAggregate> rootCommandHandler,
        ISnapshotGrainFactory snapshotGrainFactory,
        IRootReducer<ConversationAggregate> rootReducer,
        ILogger<ConversationAggregateGrain> logger
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
    public Task<OperationResult> DeleteMessageAsync(
        string messageId,
        string deletedBy
    ) =>
        ExecuteAsync(
            new DeleteMessage
            {
                MessageId = messageId,
                DeletedBy = deletedBy,
            });

    /// <inheritdoc />
    public Task<OperationResult> EditMessageAsync(
        string messageId,
        string newContent,
        string editedBy
    ) =>
        ExecuteAsync(
            new EditMessage
            {
                MessageId = messageId,
                NewContent = newContent,
                EditedBy = editedBy,
            });

    /// <inheritdoc />
    public Task<OperationResult> SendMessageAsync(
        string messageId,
        string content,
        string sentBy
    ) =>
        ExecuteAsync(
            new SendMessage
            {
                MessageId = messageId,
                Content = content,
                SentBy = sentBy,
            });

    /// <inheritdoc />
    public Task<OperationResult> StartAsync(
        string conversationId,
        string channelId
    ) =>
        ExecuteAsync(
            new StartConversation
            {
                ConversationId = conversationId,
                ChannelId = channelId,
            });
}
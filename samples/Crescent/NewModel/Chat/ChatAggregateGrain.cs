using System.Threading.Tasks;

using Crescent.NewModel.Chat.Commands;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans.Runtime;


namespace Crescent.NewModel.Chat;

/// <summary>
///     Aggregate grain implementation for the chat domain.
/// </summary>
[BrookName("CRESCENT", "NEWMODEL", "CHAT")]
internal sealed class ChatAggregateGrain
    : AggregateGrainBase<ChatState>,
      IChatAggregateGrain
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatAggregateGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="brookEventConverter">Converter for domain events to/from brook events.</param>
    /// <param name="rootCommandHandler">The root command handler for processing commands.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="rootReducer">The root reducer for obtaining the reducers hash.</param>
    /// <param name="logger">Logger instance.</param>
    public ChatAggregateGrain(
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory,
        IBrookEventConverter brookEventConverter,
        IRootCommandHandler<ChatState> rootCommandHandler,
        ISnapshotGrainFactory snapshotGrainFactory,
        IRootReducer<ChatState> rootReducer,
        ILogger<ChatAggregateGrain> logger
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
    public Task<OperationResult> AddMessageAsync(
        string messageId,
        string content,
        string author
    ) =>
        ExecuteAsync(
            new AddMessage
            {
                MessageId = messageId,
                Content = content,
                Author = author,
            });

    /// <inheritdoc />
    public Task<OperationResult> CreateAsync(
        string name
    ) =>
        ExecuteAsync(
            new CreateChat
            {
                Name = name,
            });

    /// <inheritdoc />
    public Task<OperationResult> DeleteMessageAsync(
        string messageId
    ) =>
        ExecuteAsync(
            new DeleteMessage
            {
                MessageId = messageId,
            });

    /// <inheritdoc />
    public Task<OperationResult> EditMessageAsync(
        string messageId,
        string newContent
    ) =>
        ExecuteAsync(
            new EditMessage
            {
                MessageId = messageId,
                NewContent = newContent,
            });
}
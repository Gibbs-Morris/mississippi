using System;
using System.Threading;
using System.Threading.Tasks;

using Cascade.Components.Services;
using Cascade.WebApi.Controllers.Contracts;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace Cascade.WebApi.Controllers;

/// <summary>
///     API controller for chat operations.
/// </summary>
/// <remarks>
///     This controller exposes chat operations as RESTful endpoints for WASM clients.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
internal sealed class ChatController : ControllerBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatController" /> class.
    /// </summary>
    /// <param name="chatService">The chat service.</param>
    /// <param name="logger">The logger.</param>
    public ChatController(
        IChatService chatService,
        ILogger<ChatController> logger
    )
    {
        ChatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IChatService ChatService { get; }

    private ILogger<ChatController> Logger { get; }

    /// <summary>
    ///     Creates a new channel.
    /// </summary>
    /// <param name="request">The create channel request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created channel ID.</returns>
    [HttpPost("channels")]
    [ProducesResponseType(typeof(CreateChannelResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateChannelAsync(
        [FromBody] CreateChannelRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            string channelId = await ChatService.CreateChannelAsync(
                request.ChannelName,
                null,
                cancellationToken);

            ChatControllerLoggerExtensions.ChannelCreated(Logger, channelId, request.ChannelName);

            Uri locationUri = new($"/api/chat/channels/{channelId}", UriKind.Relative);
            return Created(locationUri, new CreateChannelResponse(channelId));
        }
        catch (ChatOperationException ex)
        {
            ChatControllerLoggerExtensions.ChannelCreationFailed(Logger, request.ChannelName, ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    ///     Joins a channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("channels/{channelId}/join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinChannelAsync(
        string channelId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await ChatService.JoinChannelAsync(channelId, cancellationToken);

            ChatControllerLoggerExtensions.ChannelJoined(Logger, channelId);

            return NoContent();
        }
        catch (ChatOperationException ex)
        {
            ChatControllerLoggerExtensions.ChannelJoinFailed(Logger, channelId, ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    ///     Leaves a channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("channels/{channelId}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LeaveChannelAsync(
        string channelId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await ChatService.LeaveChannelAsync(channelId, cancellationToken);

            ChatControllerLoggerExtensions.ChannelLeft(Logger, channelId);

            return NoContent();
        }
        catch (ChatOperationException ex)
        {
            ChatControllerLoggerExtensions.ChannelLeaveFailed(Logger, channelId, ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    ///     Sends a message to a channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="request">The send message request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("channels/{channelId}/messages")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessageAsync(
        string channelId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await ChatService.SendMessageAsync(channelId, request.Content, cancellationToken);

            ChatControllerLoggerExtensions.MessageSent(Logger, channelId);

            return NoContent();
        }
        catch (ChatOperationException ex)
        {
            ChatControllerLoggerExtensions.MessageSendFailed(Logger, channelId, ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }
}

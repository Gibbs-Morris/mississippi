using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Cascade.Components.Services;


namespace Cascade.WebApi.Client.Services;

/// <summary>
///     WASM implementation of <see cref="IChatService" /> that calls HTTP API endpoints.
/// </summary>
internal sealed class HttpChatService : IChatService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="HttpChatService" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    /// <param name="userContext">The user context for authentication.</param>
    public HttpChatService(
        HttpClient httpClient,
        IUserContext userContext
    )
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        UserContext = (WasmUserContext)(userContext ?? throw new ArgumentNullException(nameof(userContext)));
    }

    private HttpClient HttpClient { get; }

    private WasmUserContext UserContext { get; }

    /// <inheritdoc />
    public async Task<string> CreateChannelAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default
    )
    {
        var request = new { Name = name, Description = description, UserId = UserContext.UserId };
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "api/chat/channels",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        CreateChannelResponse? result =
            await response.Content.ReadFromJsonAsync<CreateChannelResponse>(cancellationToken);
        return result?.ChannelId ?? throw new ChatOperationException("Failed to create channel");
    }

    /// <inheritdoc />
    public async Task JoinChannelAsync(
        string channelId,
        CancellationToken cancellationToken = default
    )
    {
        var request = new { UserId = UserContext.UserId };
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            $"api/chat/channels/{channelId}/join",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task LeaveChannelAsync(
        string channelId,
        CancellationToken cancellationToken = default
    )
    {
        var request = new { UserId = UserContext.UserId };
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            $"api/chat/channels/{channelId}/leave",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task SendMessageAsync(
        string channelId,
        string content,
        CancellationToken cancellationToken = default
    )
    {
        var request = new { UserId = UserContext.UserId, Content = content };
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            $"api/chat/channels/{channelId}/messages",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private sealed record CreateChannelResponse(string ChannelId);
}

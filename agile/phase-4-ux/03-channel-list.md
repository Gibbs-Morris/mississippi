# Task 4.3: Channel List

**Status**: ⬜ Not Started  
**Depends On**: [4.1 Subscription Service](./01-subscription-service.md), [4.2 Login Page](./02-login-page.md)

## Goal

Create the channel list sidebar showing all channels the user belongs to, with real-time updates when channels are added/renamed.

## Acceptance Criteria

- [ ] Sidebar component showing user's channels
- [ ] Real-time updates via `IProjectionSubscriber<UserChannelListProjection>`
- [ ] Channel selection highlights current channel
- [ ] "Create Channel" button/modal
- [ ] "Join Channel" capability (search/list available channels)
- [ ] Loading and empty states
- [ ] L0 tests for component logic

## Component Design

### ChannelList.razor

```razor
@inject IProjectionSubscriber<UserChannelListProjection> ChannelList
@inject IUserSession UserSession
@implements IAsyncDisposable

<div class="channel-list">
    <div class="channel-list-header">
        <h3>Channels</h3>
        <button class="btn btn-sm btn-outline-primary" @onclick="ShowCreateModal">
            <i class="bi bi-plus"></i>
        </button>
    </div>
    
    @if (!ChannelList.IsLoaded)
    {
        <div class="loading">Loading channels...</div>
    }
    else if (ChannelList.Current?.Channels.Count == 0)
    {
        <div class="empty-state">
            <p>No channels yet.</p>
            <button class="btn btn-primary" @onclick="ShowCreateModal">Create your first channel</button>
        </div>
    }
    else
    {
        <ul class="channel-items">
            @foreach (var channel in ChannelList.Current!.Channels)
            {
                <li class="channel-item @(SelectedChannelId == channel.ChannelId ? "selected" : "")"
                    @onclick="() => SelectChannel(channel.ChannelId)">
                    <span class="channel-name"># @channel.Name</span>
                    @if (channel.UnreadCount > 0)
                    {
                        <span class="badge">@channel.UnreadCount</span>
                    }
                </li>
            }
        </ul>
    }
</div>

@if (showCreateModal)
{
    <CreateChannelModal OnCreated="OnChannelCreated" OnClose="() => showCreateModal = false" />
}

@code {
    [Parameter] public string? SelectedChannelId { get; set; }
    [Parameter] public EventCallback<string> OnChannelSelected { get; set; }
    
    private bool showCreateModal;
    
    protected override async Task OnInitializedAsync()
    {
        ChannelList.OnChanged += StateHasChanged;
        await ChannelList.SubscribeAsync(UserSession.UserId!);
    }
    
    private async Task SelectChannel(string channelId)
    {
        await OnChannelSelected.InvokeAsync(channelId);
    }
    
    private void ShowCreateModal() => showCreateModal = true;
    
    private void OnChannelCreated(string channelId)
    {
        showCreateModal = false;
        _ = SelectChannel(channelId);
    }
    
    public async ValueTask DisposeAsync()
    {
        ChannelList.OnChanged -= StateHasChanged;
        await ChannelList.DisposeAsync();
    }
}
```

### CreateChannelModal.razor

```razor
@inject IChatService ChatService

<div class="modal-backdrop" @onclick="Close">
    <div class="modal-content" @onclick:stopPropagation="true">
        <h4>Create Channel</h4>
        
        <EditForm Model="model" OnValidSubmit="HandleSubmit">
            <DataAnnotationsValidator />
            
            <div class="form-group">
                <label>Channel Name</label>
                <InputText @bind-Value="model.Name" class="form-control" placeholder="e.g., general" />
                <ValidationMessage For="() => model.Name" />
            </div>
            
            <div class="form-group">
                <label>Description (optional)</label>
                <InputTextArea @bind-Value="model.Description" class="form-control" />
            </div>
            
            @if (error is not null)
            {
                <div class="alert alert-danger">@error</div>
            }
            
            <div class="modal-actions">
                <button type="button" class="btn btn-secondary" @onclick="Close">Cancel</button>
                <button type="submit" class="btn btn-primary" disabled="@isLoading">Create</button>
            </div>
        </EditForm>
    </div>
</div>

@code {
    [Parameter] public EventCallback<string> OnCreated { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    
    private CreateChannelModel model = new();
    private bool isLoading;
    private string? error;
    
    private async Task HandleSubmit()
    {
        isLoading = true;
        error = null;
        
        try
        {
            string channelId = await ChatService.CreateChannelAsync(model.Name, model.Description);
            await OnCreated.InvokeAsync(channelId);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
    
    private async Task Close() => await OnClose.InvokeAsync();
    
    public class CreateChannelModel
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = "";
        
        [StringLength(200)]
        public string? Description { get; set; }
    }
}
```

### IChatService

```csharp
/// <summary>
/// Facade for chat operations (wraps grain calls).
/// </summary>
public interface IChatService
{
    Task<string> CreateChannelAsync(string name, string? description, CancellationToken ct = default);
    Task JoinChannelAsync(string channelId, CancellationToken ct = default);
    Task LeaveChannelAsync(string channelId, CancellationToken ct = default);
    Task SendMessageAsync(string channelId, string content, CancellationToken ct = default);
    // ... more operations
}
```

## TDD Steps

1. **Red**: Create `ChatServiceTests`
   - Test: `CreateChannelAsync_CallsChannelGrainAndUserGrain`
   - Test: `JoinChannelAsync_AddsUserToChannel`

2. **Green**: Implement `ChatService`

3. **Red**: Create `ChannelListTests` (bUnit)
   - Test: `Renders_LoadingState_WhenNotLoaded`
   - Test: `Renders_EmptyState_WhenNoChannels`
   - Test: `Renders_ChannelItems_WhenLoaded`
   - Test: `SelectChannel_InvokesCallback`

4. **Green**: Implement ChannelList.razor

## Files to Create

- `samples/Cascade/Cascade.Server/Components/Shared/ChannelList.razor`
- `samples/Cascade/Cascade.Server/Components/Shared/CreateChannelModal.razor`
- `samples/Cascade/Cascade.Server/Components/Services/IChatService.cs`
- `samples/Cascade/Cascade.Server/Components/Services/ChatService.cs`
- `samples/Cascade/Cascade.Domain.L0Tests/Services/ChatServiceTests.cs`

## CSS Classes (app.css)

```css
.channel-list {
    width: 250px;
    background: #2c2f33;
    color: white;
    padding: 1rem;
}

.channel-item {
    padding: 0.5rem;
    cursor: pointer;
    border-radius: 4px;
}

.channel-item:hover {
    background: #36393f;
}

.channel-item.selected {
    background: #7289da;
}
```

## Notes

- Channel list subscribes to user-specific projection (not global)
- Real-time updates work because projection reducer handles membership events
- Consider virtual scrolling for very long channel lists (future optimization)

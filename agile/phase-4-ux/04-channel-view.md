# Task 4.4: Channel View (Atomic Design)

**Status**: ⬜ Not Started  
**Depends On**: [4.1 Subscription Service](./01-subscription-service.md), [4.3 Channel List](./03-channel-list.md)

## Goal

Create the main channel view **organism** showing message history with real-time updates and message input. Follows **atomic design**: state flows down, commands flow up.

## Atomic Design Structure

```
┌─────────────────────────────────────────────────────────────────────────┐
│ ChannelView.razor (ORGANISM)                                            │
│   - Holds state (projection data)                                       │
│   - Dispatches commands to aggregates                                   │
│                                                                         │
│   ┌─────────────────────────────────────────────────────────────────┐   │
│   │ MessageList (iterates)                                          │   │
│   │   ┌─────────────────────────────────────────────────────────┐   │   │
│   │   │ MessageItem.razor (MOLECULE)                            │   │   │
│   │   │   ┌─────────────┐  ┌────────────┐  ┌────────────────┐  │   │   │
│   │   │   │ Avatar.razor│  │ AuthorName │  │ MessageContent │  │   │   │
│   │   │   │   (ATOM)    │  │   (ATOM)   │  │    (ATOM)      │  │   │   │
│   │   │   └─────────────┘  └────────────┘  └────────────────┘  │   │   │
│   │   └─────────────────────────────────────────────────────────┘   │   │
│   └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│   ┌─────────────────────────────────────────────────────────────────┐   │
│   │ MessageComposer.razor (MOLECULE)                                │   │
│   │   ┌──────────────────────────┐  ┌────────────────────────────┐ │   │
│   │   │ MessageInput.razor (ATOM)│  │ SendButton.razor (ATOM)    │ │   │
│   │   └──────────────────────────┘  └────────────────────────────┘ │   │
│   │           │                              ▲                      │   │
│   │           └──────────────────────────────┘                      │   │
│   │                    EventCallback<string> OnSend                 │   │
│   └─────────────────────────────────────────────────────────────────┘   │
│                                  │                                      │
│                                  ▼                                      │
│          ChannelView handles OnSend → dispatches SendMessageCommand     │
└─────────────────────────────────────────────────────────────────────────┘
```

## Acceptance Criteria

- [ ] **Follows atomic design**: organism contains molecules, molecules contain atoms
- [ ] **State flows down** via `[Parameter]` properties to child components
- [ ] **Commands flow up** via `EventCallback` from child → parent → aggregate
- [ ] Message history displays all messages in channel
- [ ] Real-time updates via `IProjectionSubscriber<ChannelMessagesProjection>`
- [ ] New messages appear at bottom, auto-scroll
- [ ] Message input sends via `EventCallback` to parent
- [ ] Messages show author name, timestamp, content
- [ ] Member list sidebar with online indicators
- [ ] Loading state while fetching
- [ ] L0 tests for component logic

## Component Design (Atomic Approach)

### ChannelView.razor (Organism - Owns State & Commands)

```razor
@inject IProjectionSubscriber<ChannelMessagesProjection> Messages
@inject IProjectionSubscriber<ChannelMemberListProjection> Members
@inject IGrainFactory GrainFactory
@inject IUserSession UserSession
@implements IAsyncDisposable

<div class="channel-view">
    <div class="channel-header">
        <h2># @(Messages.Current?.ChannelName ?? "Loading...")</h2>
        <span class="member-count">@(Members.Current?.Members.Count ?? 0) members</span>
    </div>
    
    <div class="channel-content">
        <div class="message-area" @ref="messageContainer">
            @if (!Messages.IsLoaded)
            {
                <LoadingSpinner />
            }
            else if (Messages.Current?.Messages.Count == 0)
            {
                <EmptyState Message="No messages yet. Start the conversation!" />
            }
            else
            {
                @foreach (var message in Messages.Current!.Messages)
                {
                    <MessageItem 
                        Message="message" 
                        IsOwn="message.AuthorUserId == UserSession.UserId" />
                }
            }
        </div>
        
        <MemberList Members="Members.Current?.Members ?? ImmutableList<MemberInfo>.Empty" />
    </div>
    
    <!-- MessageComposer raises OnSend event, parent handles command dispatch -->
    <MessageComposer OnSend="HandleSendMessage" IsSending="@isSending" />
</div>

@code {
    [Parameter, EditorRequired] public string ChannelId { get; set; } = "";
    
    private bool isSending;
    private ElementReference messageContainer;
    
    protected override async Task OnInitializedAsync()
    {
        Messages.OnChanged += OnMessagesChanged;
        Members.OnChanged += StateHasChanged;
        
        await Task.WhenAll(
            Messages.SubscribeAsync(ChannelId),
            Members.SubscribeAsync(ChannelId)
        );
    }
    
    private async void OnMessagesChanged()
    {
        await InvokeAsync(StateHasChanged);
        await ScrollToBottomAsync();
    }
    
    /// <summary>
    /// Command handler - receives command from child, dispatches to aggregate.
    /// This is where "commands flow up" meets "dispatch to aggregate".
    /// </summary>
    private async Task HandleSendMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        
        isSending = true;
        try
        {
            // Dispatch command to aggregate grain
            var channelAggregate = GrainFactory.GetGrain<IChannelAggregate>(ChannelId);
            await channelAggregate.HandleAsync(new SendMessageCommand(
                AuthorId: UserSession.UserId,
                Content: content));
            
            // Projection will update automatically via SignalR subscription
        }
        finally
        {
            isSending = false;
        }
    }
    
    private async Task ScrollToBottomAsync()
    {
        await Task.Delay(50); // Allow render
        // await JSRuntime.InvokeVoidAsync("scrollToBottom", messageContainer);
    }
    
    public async ValueTask DisposeAsync()
    {
        Messages.OnChanged -= OnMessagesChanged;
        Members.OnChanged -= StateHasChanged;
        
        await Messages.DisposeAsync();
        await Members.DisposeAsync();
    }
}
```

### MessageItem.razor (Molecule)

```razor
<div class="message-item @(IsOwn ? "own" : "")">
    <Avatar UserName="@Message.AuthorDisplayName" />
    
    <div class="message-body">
        <div class="message-header">
            <span class="author">@Message.AuthorDisplayName</span>
            <span class="timestamp">@Message.SentAt.ToLocalTime().ToString("HH:mm")</span>
            @if (Message.IsEdited)
            {
                <span class="edited">(edited)</span>
            }
        </div>
        
        @if (Message.IsDeleted)
        {
            <div class="message-content deleted">
                <em>This message was deleted</em>
            </div>
        }
        else
        {
            <div class="message-content">
                @Message.Content
            </div>
        }
    </div>
</div>

@code {
    /// <summary>State flows DOWN via Parameter</summary>
    [Parameter, EditorRequired] public MessageView Message { get; set; } = default!;
    [Parameter] public bool IsOwn { get; set; }
}
```

### MessageComposer.razor (Molecule - Commands Flow Up)

```razor
<div class="message-composer">
    <MessageInput 
        Value="@messageContent" 
        OnValueChanged="@(v => messageContent = v)"
        OnEnterPressed="@HandleSend"
        Placeholder="Type a message..." />
    
    <SendButton OnClick="@HandleSend" IsDisabled="@(IsSending || string.IsNullOrWhiteSpace(messageContent))" />
</div>

@code {
    /// <summary>Commands flow UP via EventCallback</summary>
    [Parameter, EditorRequired] public EventCallback<string> OnSend { get; set; }
    [Parameter] public bool IsSending { get; set; }
    
    private string messageContent = "";
    
    private async Task HandleSend()
    {
        if (string.IsNullOrWhiteSpace(messageContent)) return;
        
        var content = messageContent;
        messageContent = ""; // Clear for UX
        
        // Raise event to parent - parent dispatches command to aggregate
        await OnSend.InvokeAsync(content);
    }
}
```

### MessageInput.razor (Atom)

```razor
<input type="text" 
       value="@Value"
       @oninput="HandleInput"
       @onkeypress="HandleKeyPress" 
       placeholder="@Placeholder"
       class="message-input-field" />

@code {
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public EventCallback<string> OnValueChanged { get; set; }
    [Parameter] public EventCallback OnEnterPressed { get; set; }
    [Parameter] public string Placeholder { get; set; } = "";
    
    private async Task HandleInput(ChangeEventArgs e)
    {
        await OnValueChanged.InvokeAsync(e.Value?.ToString() ?? "");
    }
    
    private async Task HandleKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await OnEnterPressed.InvokeAsync();
        }
    }
}
```

### SendButton.razor (Atom)

```razor
<button class="btn btn-primary send-button" 
        @onclick="HandleClick" 
        disabled="@IsDisabled">
    Send
</button>

@code {
    [Parameter, EditorRequired] public EventCallback OnClick { get; set; }
    [Parameter] public bool IsDisabled { get; set; }
    
    private async Task HandleClick() => await OnClick.InvokeAsync();
}
```

### MemberList.razor (Organism)

```razor
<div class="member-list">
    <h4>Members</h4>
    
    @if (Members is null || Members.Count == 0)
    {
        <p class="empty">No members</p>
    }
    else
    {
        <ul>
            @foreach (var member in Members.OrderByDescending(m => m.IsOnline))
            {
                <li class="member-item">
                    <OnlineIndicator IsOnline="member.IsOnline" />
                    <span class="member-name">@member.DisplayName</span>
                </li>
            }
        </ul>
    }
</div>

@code {
    [Parameter] public IReadOnlyList<MemberView>? Members { get; set; }
}
```

### OnlineIndicator.razor

```razor
<span class="online-indicator @(IsOnline ? "online" : "offline")"></span>

@code {
    [Parameter] public bool IsOnline { get; set; }
}
```

## CSS Additions

```css
.channel-view {
    display: flex;
    flex-direction: column;
    height: 100%;
}

.message-area {
    flex: 1;
    overflow-y: auto;
    padding: 1rem;
}

.message-item {
    margin-bottom: 1rem;
    padding: 0.5rem;
    background: #36393f;
    border-radius: 4px;
}

.message-item.own {
    background: #7289da;
}

.message-input {
    display: flex;
    padding: 1rem;
    gap: 0.5rem;
    border-top: 1px solid #444;
}

.message-input input {
    flex: 1;
}

.online-indicator {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    display: inline-block;
    margin-right: 0.5rem;
}

.online-indicator.online {
    background: #43b581;
}

.online-indicator.offline {
    background: #747f8d;
}
```

## TDD Steps

1. **Red**: Create `ChannelViewTests` (bUnit)
   - Test: `Renders_LoadingState_WhenMessagesNotLoaded`
   - Test: `Renders_Messages_WhenLoaded`
   - Test: `SendMessage_ClearsInputAndCallsService`
   - Test: `EnterKey_SendsMessage`

2. **Green**: Implement ChannelView.razor

3. **Red**: Create `MessageItemTests`
   - Test: `Renders_AuthorAndContent`
   - Test: `Renders_EditedIndicator_WhenEdited`
   - Test: `Renders_DeletedMessage_WhenDeleted`

4. **Green**: Implement MessageItem.razor

5. **Red**: Create `MessageComposerTests`
   - Test: `OnSend_RaisedWithContent_WhenSendClicked`
   - Test: `ClearsInput_AfterSend`
   - Test: `DisablesSendButton_WhenIsSending`

6. **Green**: Implement MessageComposer.razor and child atoms

## Files to Create (Atomic Organization)

### Atoms
- `samples/Cascade/Cascade.Server/Components/Atoms/Avatar.razor`
- `samples/Cascade/Cascade.Server/Components/Atoms/OnlineIndicator.razor`
- `samples/Cascade/Cascade.Server/Components/Atoms/MessageInput.razor`
- `samples/Cascade/Cascade.Server/Components/Atoms/SendButton.razor`
- `samples/Cascade/Cascade.Server/Components/Atoms/LoadingSpinner.razor`
- `samples/Cascade/Cascade.Server/Components/Atoms/EmptyState.razor`

### Molecules
- `samples/Cascade/Cascade.Server/Components/Molecules/MessageItem.razor`
- `samples/Cascade/Cascade.Server/Components/Molecules/MessageComposer.razor`
- `samples/Cascade/Cascade.Server/Components/Molecules/UserItem.razor`

### Organisms
- `samples/Cascade/Cascade.Server/Components/Organisms/ChannelView.razor`
- `samples/Cascade/Cascade.Server/Components/Organisms/MemberList.razor`

### Tests
- `samples/Cascade/Cascade.Server.L0Tests/Components/ChannelViewTests.cs`
- `samples/Cascade/Cascade.Server.L0Tests/Components/MessageItemTests.cs`
- `samples/Cascade/Cascade.Server.L0Tests/Components/MessageComposerTests.cs`

## Notes

- Two subscriptions per channel view: messages + members
- **Commands flow up**: `MessageComposer` → `ChannelView` → `IChannelAggregate`
- **State flows down**: `ChannelView` → `MessageItem` via parameters
- Auto-scroll on new message uses JS interop
- Consider virtualization for channels with many messages
- Optimistic UI: clear input immediately, restore on failure

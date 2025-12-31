# Phase 4: UX – Blazor Components (Atomic Design)

**Status**: ⬜ Not Started

## Goal

Build Blazor components for the chat UI using **atomic design principles**, including a subscription service that wraps SignalR + HTTP fetch for automatic real-time updates.

## Atomic Design Principles

- **State flows DOWN** via parameters (`[Parameter]` properties)
- **Commands flow UP** via `EventCallback` to parent components
- Components are organized in increasing complexity: Atoms → Molecules → Organisms → Pages

```
┌───────────────────────────────────────────────────────────────────┐
│                        ATOMIC DESIGN                              │
│                                                                   │
│   ┌────────────┐   composes   ┌────────────┐   composes          │
│   │   ATOMS    │ ───────────▶ │ MOLECULES  │ ───────────▶        │
│   │ (buttons,  │              │ (message   │                     │
│   │  inputs)   │              │  input box)│                     │
│   └────────────┘              └────────────┘                     │
│                                     │                             │
│                              composes│                            │
│                                     ▼                             │
│   ┌────────────┐              ┌────────────┐                     │
│   │   PAGES    │ ◀─────────── │ ORGANISMS  │                     │
│   │ (Login,    │   composes   │ (channel   │                     │
│   │  Channels) │              │  view)     │                     │
│   └────────────┘              └────────────┘                     │
└───────────────────────────────────────────────────────────────────┘

DATA FLOW:
         ┌──────────────────────────────────────────┐
         │              PAGE                        │
         │  ┌──────────────────────────────────┐   │
         │  │ State (projection data)          │   │
         │  └──────────────────────────────────┘   │
         │          │ [Parameter]                   │
         │          ▼ state flows DOWN              │
         │  ┌──────────────────────────────────┐   │
         │  │        ORGANISM                  │   │
         │  └──────────────────────────────────┘   │
         │          │                ▲              │
         │          │ [Parameter]    │ EventCallback│
         │          ▼                │ commands UP  │
         │  ┌──────────────────────────────────┐   │
         │  │         MOLECULE                 │   │
         │  └──────────────────────────────────┘   │
         └──────────────────────────────────────────┘
```

## Command Dispatcher Pattern

Pages are responsible for sending commands to aggregates. Use a standard pattern:

```csharp
// Page handles the command dispatch
private async Task HandleSendMessage(string content)
{
    // Get the aggregate grain via IGrainFactory
    var aggregate = GrainFactory.GetGrain<IConversationAggregate>(conversationId);
    
    // Send command through command handler
    await aggregate.HandleAsync(new SendMessageCommand(UserId, content));
    
    // Projection will update automatically via SignalR subscription
}
```

## Component Structure (Atomic Organization)

```
Cascade.Server/
└── Components/
    ├── Atoms/                       # Smallest building blocks
    │   ├── OnlineIndicator.razor    # Green/gray dot
    │   ├── Avatar.razor             # User avatar circle
    │   ├── SendButton.razor         # Styled send button
    │   └── MessageInput.razor       # Text input field
    ├── Molecules/                   # Combinations of atoms
    │   ├── MessageItem.razor        # Avatar + content + timestamp
    │   ├── ChannelItem.razor        # Channel name + unread count
    │   ├── UserItem.razor           # Avatar + name + online indicator
    │   └── MessageComposer.razor    # Input + send button
    ├── Organisms/                   # Complex, self-contained sections
    │   ├── ChannelList.razor        # Sidebar with channel items
    │   ├── ChannelView.razor        # Message history + composer
    │   ├── MemberList.razor         # List of members with status
    │   └── ConversationView.razor   # DM message history + composer
    ├── Pages/                       # Full page layouts
    │   ├── Login.razor              # Username entry
    │   └── Channels.razor           # Main chat layout
    └── Services/
        ├── IProjectionSubscriber.cs # Interface for auto-updating projections
        ├── ProjectionSubscriber.cs  # SignalR + HTTP implementation
        ├── UserSession.cs           # Username/UserId storage (scoped)
        └── ChatApiClient.cs         # HTTP client for commands/queries
```

## Tasks

| Task | File | Status |
|------|------|--------|
| 4.1 Projection Subscription Service | [01-subscription-service.md](./01-subscription-service.md) | ⬜ |
| 4.2 Login Page | [02-login-page.md](./02-login-page.md) | ⬜ |
| 4.3 Channel List | [03-channel-list.md](./03-channel-list.md) | ⬜ |
| 4.4 Channel View | [04-channel-view.md](./04-channel-view.md) | ⬜ |

## Acceptance Criteria

- [ ] **Atomic design**: Atoms, molecules, organisms, pages folders with proper composition
- [ ] **State down, commands up**: No child component directly calls grains
- [ ] `IProjectionSubscriber<T>` provides auto-updating projection access
- [ ] Login page stores username in session (no password)
- [ ] Channel list shows user's channels with real-time updates
- [ ] Channel view shows message history with real-time new messages
- [ ] Send message works and appears in other users' views
- [ ] Online status indicators work
- [ ] All components handle loading/error states
- [ ] L0 tests for service logic

## Key UX Flows

### Login

1. User enters display name
2. System calls `RegisterUser` command (or finds existing user by name)
3. UserId stored in session
4. Redirect to Channels page

### Channel Interaction

1. User sees channel list in sidebar (subscribed via `IProjectionSubscriber<UserChannelListProjection>`)
2. User clicks channel → loads `ChannelMessagesProjection`
3. Messages stream in real-time via subscription
4. User types message → `SendMessage` command → appears instantly (optimistic UI optional)

### Real-Time Updates

1. SignalR receives `OnProjectionChangedAsync(type, entityId, version)`
2. `ProjectionSubscriber` compares version to current
3. If newer, fetches via HTTP with ETag
4. If 304, no update needed; if 200, updates `Current` and notifies component
5. Blazor re-renders via `StateHasChanged()`

## Key Design Notes

1. **Scoped Services**: `ProjectionSubscriber<T>` is scoped per circuit; manages its own SignalR connection
2. **Dispose Pattern**: Unsubscribe from SignalR on component dispose
3. **Optimistic UI**: Optional; send message appears immediately, rolls back on failure
4. **Error Handling**: Show toast/inline errors for failed commands

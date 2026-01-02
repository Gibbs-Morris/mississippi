# Task 2.4: UX Projections

**Status**: ⬜ Not Started  
**Depends On**: [2.1 User Aggregate](./01-user-aggregate.md), [2.2 Channel Aggregate](./02-channel-aggregate.md), [2.3 Conversation Aggregate](./03-conversation-aggregate.md)

## Goal

Implement UX projections optimized for read operations in the Blazor UI, following Mississippi's `UxProjectionGrainBase` patterns.

## Acceptance Criteria

- [ ] `UserChannelListProjection` - channels a user belongs to
- [ ] `ChannelMessagesProjection` - message history for a channel
- [ ] `ChannelMemberListProjection` - current members of a channel with online status
- [ ] `OnlineUsersProjection` - all currently online users (for presence indicators)
- [ ] All projections have `[SnapshotStorageName]` attributes
- [ ] All projection reducers registered
- [ ] Projection grains accessible via standard UX projection interfaces
- [ ] L0 tests for projection reducers

## Projections

### UserChannelListProjection

Shows the sidebar of channels for a specific user.

```csharp
[SnapshotStorageName("CASCADE", "CHAT", "USERCHANNELLIST")]
[GenerateSerializer]
internal sealed record UserChannelListProjection
{
    [Id(0)] public string UserId { get; init; } = string.Empty;
    [Id(1)] public ImmutableList<ChannelSummary> Channels { get; init; } = [];
}

[GenerateSerializer]
internal sealed record ChannelSummary
{
    [Id(0)] public string ChannelId { get; init; } = string.Empty;
    [Id(1)] public string Name { get; init; } = string.Empty;
    [Id(2)] public int UnreadCount { get; init; }
    [Id(3)] public DateTimeOffset? LastMessageAt { get; init; }
}
```

**Subscribes to events**: `UserJoinedChannel`, `UserLeftChannel`, `ChannelRenamed`, `MessageSent`

### ChannelMessagesProjection

Shows message history for a channel (optimized for display).

```csharp
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELMESSAGES")]
[GenerateSerializer]
internal sealed record ChannelMessagesProjection
{
    [Id(0)] public string ChannelId { get; init; } = string.Empty;
    [Id(1)] public string ChannelName { get; init; } = string.Empty;
    [Id(2)] public ImmutableList<MessageView> Messages { get; init; } = [];
    [Id(3)] public int TotalMessageCount { get; init; }
}

[GenerateSerializer]
internal sealed record MessageView
{
    [Id(0)] public string MessageId { get; init; } = string.Empty;
    [Id(1)] public string AuthorUserId { get; init; } = string.Empty;
    [Id(2)] public string AuthorDisplayName { get; init; } = string.Empty; // Denormalized
    [Id(3)] public string Content { get; init; } = string.Empty;
    [Id(4)] public DateTimeOffset SentAt { get; init; }
    [Id(5)] public bool IsEdited { get; init; }
    [Id(6)] public bool IsDeleted { get; init; }
}
```

**Subscribes to events**: `ChannelCreated`, `MessageSent`, `MessageEdited`, `MessageDeleted`

### ChannelMemberListProjection

Shows members of a channel with online status.

```csharp
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELMEMBERLIST")]
[GenerateSerializer]
internal sealed record ChannelMemberListProjection
{
    [Id(0)] public string ChannelId { get; init; } = string.Empty;
    [Id(1)] public ImmutableList<MemberView> Members { get; init; } = [];
}

[GenerateSerializer]
internal sealed record MemberView
{
    [Id(0)] public string UserId { get; init; } = string.Empty;
    [Id(1)] public string DisplayName { get; init; } = string.Empty;
    [Id(2)] public bool IsOnline { get; init; }
    [Id(3)] public DateTimeOffset JoinedAt { get; init; }
}
```

**Subscribes to events**: `MemberAdded`, `MemberRemoved`, `UserWentOnline`, `UserWentOffline`, `DisplayNameUpdated`

### OnlineUsersProjection

Global presence indicator (could be scoped to workspace in larger apps).

```csharp
[SnapshotStorageName("CASCADE", "CHAT", "ONLINEUSERS")]
[GenerateSerializer]
internal sealed record OnlineUsersProjection
{
    [Id(0)] public ImmutableHashSet<string> OnlineUserIds { get; init; } = [];
    [Id(1)] public int Count { get; init; }
}
```

**Subscribes to events**: `UserWentOnline`, `UserWentOffline`

## Cross-Aggregate Projection Challenge

Some projections need events from multiple aggregates:

- `ChannelMemberListProjection` needs `MemberAdded` (Channel) + `UserWentOnline` (User)
- `UserChannelListProjection` needs `UserJoinedChannel` (User) + `ChannelRenamed` (Channel)

**Solution Options**:

1. **Denormalize at write time**: When user goes online, also emit event to relevant channel streams
2. **Multi-stream subscription**: Projection subscribes to multiple brooks
3. **Process manager**: Saga that coordinates cross-aggregate updates

**Recommendation**: Start with Option 1 (denormalize) for simplicity in the sample.

## TDD Steps

### Per Projection

1. **Red**: Write tests for each reducer
   - Test state transformation for each subscribed event
   - Test edge cases (empty state, duplicate events)

2. **Green**: Implement reducers

3. **Red**: Write tests for projection grain (if needed)
   - Test `GetLatestAsync` returns correct data
   - Test version tracking

4. **Green**: Implement projection grain

## Files to Create

```text
samples/Cascade/Cascade.Domain/
├── Projections/
│   ├── UserChannelList/
│   │   ├── UserChannelListProjection.cs
│   │   ├── ChannelSummary.cs
│   │   ├── Reducers/
│   │   │   ├── UserJoinedChannelReducer.cs
│   │   │   └── ... (one per event)
│   │   └── UserChannelListProjectionGrain.cs
│   ├── ChannelMessages/
│   │   ├── ChannelMessagesProjection.cs
│   │   ├── MessageView.cs
│   │   ├── Reducers/
│   │   │   └── ...
│   │   └── ChannelMessagesProjectionGrain.cs
│   ├── ChannelMemberList/
│   │   └── ...
│   └── OnlineUsers/
│       └── ...

samples/Cascade/Cascade.Domain.L0Tests/
└── Projections/
    ├── UserChannelList/
    │   └── Reducers/
    │       └── ...
    └── ... (mirror structure)
```

## Registration

Add to `CascadeRegistrations`:

```csharp
// Register projection reducers
services.AddReducer<UserJoinedChannel, UserChannelListProjection, UserJoinedChannelProjectionReducer>();
// ... etc

services.AddUxProjections();
```

## Notes

- Projections are read-optimized; may denormalize data for faster queries
- Each projection tracks its own version via `BrookPosition`
- Projections are the target of real-time subscriptions from Phase 1

# Task 2.2: Channel Aggregate

**Status**: ⬜ Not Started  
**Depends On**: Phase 1 complete (or can start in parallel for domain logic)

## Goal

Implement the `Channel` aggregate representing a public chat room with members and message history.

## Acceptance Criteria

- [ ] `ChannelBrook` defined with `[BrookName("CASCADE", "CHAT", "CHANNEL")]`
- [ ] `ChannelState` record with all tracked properties including message list
- [ ] All commands implemented with validation
- [ ] All events defined with Orleans serialization
- [ ] All handlers implemented returning appropriate events or errors
- [ ] All reducers updating state correctly
- [ ] `IChannelAggregateGrain` interface defined
- [ ] `ChannelAggregateGrain` implementation
- [ ] L0 tests for each handler (valid and invalid cases)
- [ ] L0 tests for each reducer

## Domain Model

### ChannelState

```csharp
[GenerateSerializer]
internal sealed record ChannelState
{
    [Id(0)] public bool IsCreated { get; init; }
    [Id(1)] public string ChannelId { get; init; } = string.Empty;
    [Id(2)] public string Name { get; init; } = string.Empty;
    [Id(3)] public string? Description { get; init; }
    [Id(4)] public string CreatedByUserId { get; init; } = string.Empty;
    [Id(5)] public DateTimeOffset CreatedAt { get; init; }
    [Id(6)] public ImmutableHashSet<string> MemberUserIds { get; init; } = [];
    [Id(7)] public ImmutableList<ChannelMessage> Messages { get; init; } = [];
    [Id(8)] public bool IsArchived { get; init; }
}

[GenerateSerializer]
internal sealed record ChannelMessage
{
    [Id(0)] public string MessageId { get; init; } = string.Empty;
    [Id(1)] public string AuthorUserId { get; init; } = string.Empty;
    [Id(2)] public string Content { get; init; } = string.Empty;
    [Id(3)] public DateTimeOffset SentAt { get; init; }
    [Id(4)] public DateTimeOffset? EditedAt { get; init; }
    [Id(5)] public bool IsDeleted { get; init; }
}
```

### Commands

| Command | Properties | Validation |
|---------|------------|------------|
| `CreateChannel` | `ChannelId`, `Name`, `Description?`, `CreatedByUserId` | Channel must not exist |
| `RenameChannel` | `NewName`, `RenamedByUserId` | Channel must exist; name not empty |
| `AddMember` | `UserId`, `AddedByUserId` | Channel must exist; user not already member |
| `RemoveMember` | `UserId`, `RemovedByUserId` | Channel must exist; user must be member |
| `SendMessage` | `MessageId`, `AuthorUserId`, `Content` | Channel must exist; author must be member; content not empty |
| `EditMessage` | `MessageId`, `NewContent`, `EditedByUserId` | Message must exist; editor must be author |
| `DeleteMessage` | `MessageId`, `DeletedByUserId` | Message must exist; deleter must be author or admin |
| `ArchiveChannel` | `ArchivedByUserId` | Channel must exist; not already archived |

### Events

| Event | Properties |
|-------|------------|
| `ChannelCreated` | `ChannelId`, `Name`, `Description?`, `CreatedByUserId`, `CreatedAt` |
| `ChannelRenamed` | `OldName`, `NewName`, `RenamedByUserId`, `Timestamp` |
| `MemberAdded` | `UserId`, `AddedByUserId`, `AddedAt` |
| `MemberRemoved` | `UserId`, `RemovedByUserId`, `RemovedAt` |
| `MessageSent` | `MessageId`, `AuthorUserId`, `Content`, `SentAt` |
| `MessageEdited` | `MessageId`, `OldContent`, `NewContent`, `EditedAt` |
| `MessageDeleted` | `MessageId`, `DeletedByUserId`, `DeletedAt` |
| `ChannelArchived` | `ArchivedByUserId`, `ArchivedAt` |

## TDD Steps

### Handlers (Core Message Flow First)

1. **Red**: Write tests for `CreateChannelHandler`
   - `CreateChannel_WhenNotExists_ReturnsChannelCreatedEvent`
   - `CreateChannel_WhenAlreadyExists_ReturnsError`

2. **Green**: Implement handler

3. **Red**: Write tests for `SendMessageHandler`
   - `SendMessage_WhenMember_ReturnsMessageSentEvent`
   - `SendMessage_WhenNotMember_ReturnsError`
   - `SendMessage_WhenChannelNotExists_ReturnsError`
   - `SendMessage_WhenEmptyContent_ReturnsError`

4. **Green**: Implement handler

5. Repeat for remaining handlers

### Reducers

1. **Red**: Write tests for `ChannelCreatedReducer`
   - `Reduce_ChannelCreated_SetsAllProperties`

2. **Green**: Implement reducer

3. **Red**: Write tests for `MessageSentReducer`
   - `Reduce_MessageSent_AddsMessageToList`

4. **Green**: Implement reducer

5. Repeat for all reducers

## Files to Create

```
samples/Cascade/Cascade.Domain/
├── Channel/
│   ├── Commands/
│   │   ├── CreateChannel.cs
│   │   ├── RenameChannel.cs
│   │   ├── AddMember.cs
│   │   ├── RemoveMember.cs
│   │   ├── SendMessage.cs
│   │   ├── EditMessage.cs
│   │   ├── DeleteMessage.cs
│   │   └── ArchiveChannel.cs
│   ├── Events/
│   │   ├── ChannelCreated.cs
│   │   ├── ChannelRenamed.cs
│   │   ├── MemberAdded.cs
│   │   ├── MemberRemoved.cs
│   │   ├── MessageSent.cs
│   │   ├── MessageEdited.cs
│   │   ├── MessageDeleted.cs
│   │   └── ChannelArchived.cs
│   ├── Handlers/
│   │   └── ... (one per command)
│   ├── Reducers/
│   │   └── ... (one per event)
│   ├── IChannelAggregateGrain.cs
│   ├── ChannelAggregateGrain.cs
│   ├── ChannelBrook.cs
│   ├── ChannelState.cs
│   └── ChannelMessage.cs

samples/Cascade/Cascade.Domain.L0Tests/
└── Channel/
    ├── Handlers/
    │   └── ... (one test class per handler)
    └── Reducers/
        └── ... (one test class per reducer)
```

## Notes

- Channel grain key: `ChannelId` (string, e.g., "channel-general")
- Creator is automatically added as first member
- Message ordering is append-only by `SentAt`
- Soft delete for messages (set `IsDeleted = true`, preserve content for audit)
- Consider message pagination in projection, not in aggregate state

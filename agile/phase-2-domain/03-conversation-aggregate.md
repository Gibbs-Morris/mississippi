# Task 2.3: Conversation Aggregate

**Status**: ⬜ Not Started  
**Depends On**: Phase 1 complete (or can start in parallel for domain logic)

## Goal

Implement the `Conversation` aggregate representing private direct message threads between users.

## Acceptance Criteria

- [ ] `ConversationBrook` defined with `[BrookName("CASCADE", "CHAT", "CONVERSATION")]`
- [ ] `ConversationState` record with all tracked properties
- [ ] All commands implemented with validation
- [ ] All events defined with Orleans serialization
- [ ] All handlers implemented returning appropriate events or errors
- [ ] All reducers updating state correctly
- [ ] `IConversationAggregateGrain` interface defined
- [ ] `ConversationAggregateGrain` implementation
- [ ] L0 tests for each handler (valid and invalid cases)
- [ ] L0 tests for each reducer

## Domain Model

### ConversationState

```csharp
[GenerateSerializer]
internal sealed record ConversationState
{
    [Id(0)] public bool IsStarted { get; init; }
    [Id(1)] public string ConversationId { get; init; } = string.Empty;
    [Id(2)] public ImmutableHashSet<string> ParticipantUserIds { get; init; } = [];
    [Id(3)] public ImmutableList<DirectMessage> Messages { get; init; } = [];
    [Id(4)] public DateTimeOffset StartedAt { get; init; }
    [Id(5)] public string StartedByUserId { get; init; } = string.Empty;
}

[GenerateSerializer]
internal sealed record DirectMessage
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
| --------- | ------------ | ------------ |
| `StartConversation` | `ConversationId`, `InitiatorUserId`, `ParticipantUserIds` | Must have 2+ participants; conversation must not exist |
| `SendDirectMessage` | `MessageId`, `AuthorUserId`, `Content` | Conversation must exist; author must be participant; content not empty |
| `EditDirectMessage` | `MessageId`, `NewContent`, `EditedByUserId` | Message must exist; editor must be author |
| `DeleteDirectMessage` | `MessageId`, `DeletedByUserId` | Message must exist; deleter must be author |
| `AddParticipant` | `UserId`, `AddedByUserId` | Conversation must exist; user not already participant |
| `LeaveConversation` | `UserId` | User must be participant; cannot leave if only 2 participants |

### Events

| Event | Properties |
| ------- | ------------ |
| `ConversationStarted` | `ConversationId`, `ParticipantUserIds`, `StartedByUserId`, `StartedAt` |
| `DirectMessageSent` | `MessageId`, `AuthorUserId`, `Content`, `SentAt` |
| `DirectMessageEdited` | `MessageId`, `OldContent`, `NewContent`, `EditedAt` |
| `DirectMessageDeleted` | `MessageId`, `DeletedByUserId`, `DeletedAt` |
| `ParticipantAdded` | `UserId`, `AddedByUserId`, `AddedAt` |
| `ParticipantLeft` | `UserId`, `LeftAt` |

## Conversation ID Strategy

For 1:1 DMs, derive a deterministic conversation ID from participant IDs:

```csharp
// Sort user IDs alphabetically, join with ":"
string conversationId = string.Join(":", participantUserIds.Order());
// e.g., "user-alice:user-bob"
```

For group DMs, use a generated ID like `"dm-{ulid}"`.

## TDD Steps

### Handlers

1. **Red**: Write tests for `StartConversationHandler`
   - `StartConversation_WithTwoParticipants_ReturnsConversationStartedEvent`
   - `StartConversation_WhenAlreadyStarted_ReturnsError`
   - `StartConversation_WithOneParticipant_ReturnsError`

2. **Green**: Implement handler

3. **Red**: Write tests for `SendDirectMessageHandler`
   - `SendDirectMessage_WhenParticipant_ReturnsDirectMessageSentEvent`
   - `SendDirectMessage_WhenNotParticipant_ReturnsError`
   - `SendDirectMessage_WhenConversationNotStarted_ReturnsError`

4. **Green**: Implement handler

5. Repeat for remaining handlers

### Reducers

1. **Red**: Write tests for `ConversationStartedReducer`
   - `Reduce_ConversationStarted_SetsAllProperties`

2. **Green**: Implement reducer

3. Repeat for all reducers

## Files to Create

```text
samples/Cascade/Cascade.Domain/
├── Conversation/
│   ├── Commands/
│   │   ├── StartConversation.cs
│   │   ├── SendDirectMessage.cs
│   │   ├── EditDirectMessage.cs
│   │   ├── DeleteDirectMessage.cs
│   │   ├── AddParticipant.cs
│   │   └── LeaveConversation.cs
│   ├── Events/
│   │   ├── ConversationStarted.cs
│   │   ├── DirectMessageSent.cs
│   │   ├── DirectMessageEdited.cs
│   │   ├── DirectMessageDeleted.cs
│   │   ├── ParticipantAdded.cs
│   │   └── ParticipantLeft.cs
│   ├── Handlers/
│   │   └── ... (one per command)
│   ├── Reducers/
│   │   └── ... (one per event)
│   ├── IConversationAggregateGrain.cs
│   ├── ConversationAggregateGrain.cs
│   ├── ConversationBrook.cs
│   ├── ConversationState.cs
│   └── DirectMessage.cs

samples/Cascade/Cascade.Domain.L0Tests/
└── Conversation/
    ├── Handlers/
    │   └── ... (one test class per handler)
    └── Reducers/
        └── ... (one test class per reducer)
```

## Notes

- Conversation differs from Channel: Channels are public/named; Conversations are private/between specific users
- 1:1 DM uses deterministic ID so two users always get the same conversation
- Group DMs (3+ people) use generated IDs
- Message structure is similar to Channel but kept separate for domain clarity

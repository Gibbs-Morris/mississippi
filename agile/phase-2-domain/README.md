# Phase 2: Domain – Cascade Chat Aggregates

**Status**: ⬜ Not Started

## Goal

Create `Cascade.Domain` library with three aggregates (User, Channel, Conversation) and UX projections, following Mississippi patterns from the [Counter sample](../samples/Crescent/ConsoleApp/Counter/).

## Project Structure

```
samples/Cascade/
└── Cascade.Domain/
    ├── User/
    │   ├── Commands/
    │   ├── Events/
    │   ├── Handlers/
    │   ├── Reducers/
    │   ├── IUserAggregateGrain.cs
    │   ├── UserAggregateGrain.cs
    │   ├── UserBrook.cs
    │   └── UserState.cs
    ├── Channel/
    │   ├── Commands/
    │   ├── Events/
    │   ├── Handlers/
    │   ├── Reducers/
    │   ├── IChannelAggregateGrain.cs
    │   ├── ChannelAggregateGrain.cs
    │   ├── ChannelBrook.cs
    │   └── ChannelState.cs
    ├── Conversation/
    │   ├── Commands/
    │   ├── Events/
    │   ├── Handlers/
    │   ├── Reducers/
    │   ├── IConversationAggregateGrain.cs
    │   ├── ConversationAggregateGrain.cs
    │   ├── ConversationBrook.cs
    │   └── ConversationState.cs
    ├── Projections/
    │   ├── UserChannelList/
    │   ├── ChannelMessages/
    │   ├── ChannelMemberList/
    │   └── OnlineUsers/
    └── CascadeRegistrations.cs
```

## Tasks

| Task | File | Status |
|------|------|--------|
| 2.1 User Aggregate | [01-user-aggregate.md](./01-user-aggregate.md) | ⬜ |
| 2.2 Channel Aggregate | [02-channel-aggregate.md](./02-channel-aggregate.md) | ⬜ |
| 2.3 Conversation Aggregate | [03-conversation-aggregate.md](./03-conversation-aggregate.md) | ⬜ |
| 2.4 UX Projections | [04-projections.md](./04-projections.md) | ⬜ |

## Acceptance Criteria

- [ ] All three aggregates implemented with commands, events, handlers, reducers
- [ ] Brooks defined with proper naming (`CASCADE.CHAT.*`)
- [ ] All event types serializable with Orleans `[GenerateSerializer]`
- [ ] `CascadeRegistrations.AddCascadeDomain()` registers all DI dependencies
- [ ] UX projections for channel list, message history, member list, online users
- [ ] L0 tests for all command handlers (valid/invalid states)
- [ ] L0 tests for all reducers (state transformation correctness)
- [ ] Project builds with zero warnings

## New Projects

- `samples/Cascade/Cascade.Domain/`
- `samples/Cascade/Cascade.Domain.L0Tests/`

## Domain Model Overview

### User

- Represents a chat participant
- Tracks: display name, online status, channels they belong to
- Commands: `RegisterUser`, `UpdateDisplayName`, `SetOnlineStatus`, `JoinChannel`, `LeaveChannel`

### Channel

- Public chat room with multiple members
- Tracks: name, member list, message history
- Commands: `CreateChannel`, `RenameChannel`, `AddMember`, `RemoveMember`, `SendMessage`, `EditMessage`, `DeleteMessage`

### Conversation

- Private 1:1 or small group DM thread
- Tracks: participant list, message history
- Commands: `StartConversation`, `SendDirectMessage`, `AddParticipant`

## Key Patterns

- Use `[BrookName("CASCADE", "CHAT", "XXX")]` attribute
- Use `[EventStorageName("CASCADE", "CHAT", "XXXEVENT")]` attribute
- Use `[SnapshotStorageName("CASCADE", "CHAT", "XXX")]` for projections
- Follow `CounterRegistrations` pattern for DI setup

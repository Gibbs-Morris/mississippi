# Task 2.1: User Aggregate

**Status**: ⬜ Not Started  
**Depends On**: Phase 1 complete (or can start in parallel for domain logic)

## Goal

Implement the `User` aggregate representing a chat participant, tracking their profile, online status, and channel memberships.

## Acceptance Criteria

- [ ] `UserBrook` defined with `[BrookName("CASCADE", "CHAT", "USER")]`
- [ ] `UserState` record with all tracked properties
- [ ] All commands implemented with validation
- [ ] All events defined with Orleans serialization
- [ ] All handlers implemented returning appropriate events or errors
- [ ] All reducers updating state correctly
- [ ] `IUserAggregateGrain` interface defined
- [ ] `UserAggregateGrain` implementation
- [ ] L0 tests for each handler (valid and invalid cases)
- [ ] L0 tests for each reducer

## Domain Model

### UserState

```csharp
[GenerateSerializer]
internal sealed record UserState
{
    [Id(0)] public bool IsRegistered { get; init; }
    [Id(1)] public string UserId { get; init; } = string.Empty;
    [Id(2)] public string DisplayName { get; init; } = string.Empty;
    [Id(3)] public bool IsOnline { get; init; }
    [Id(4)] public ImmutableHashSet<string> ChannelIds { get; init; } = [];
    [Id(5)] public DateTimeOffset RegisteredAt { get; init; }
    [Id(6)] public DateTimeOffset? LastSeenAt { get; init; }
}
```

### Commands

| Command | Properties | Validation |
|---------|------------|------------|
| `RegisterUser` | `UserId`, `DisplayName` | User must not already be registered |
| `UpdateDisplayName` | `NewDisplayName` | User must be registered; name not empty |
| `SetOnlineStatus` | `IsOnline` | User must be registered |
| `JoinChannel` | `ChannelId` | User must be registered; not already in channel |
| `LeaveChannel` | `ChannelId` | User must be registered; must be in channel |

### Events

| Event | Properties |
|-------|------------|
| `UserRegistered` | `UserId`, `DisplayName`, `RegisteredAt` |
| `DisplayNameUpdated` | `OldDisplayName`, `NewDisplayName` |
| `UserWentOnline` | `Timestamp` |
| `UserWentOffline` | `Timestamp` |
| `UserJoinedChannel` | `ChannelId`, `JoinedAt` |
| `UserLeftChannel` | `ChannelId`, `LeftAt` |

## TDD Steps

### Handlers

1. **Red**: Write tests for `RegisterUserHandler`
   - `RegisterUser_WhenNotRegistered_ReturnsUserRegisteredEvent`
   - `RegisterUser_WhenAlreadyRegistered_ReturnsError`

2. **Green**: Implement handler with minimal validation

3. **Red**: Write tests for `UpdateDisplayNameHandler`
   - `UpdateDisplayName_WhenRegistered_ReturnsDisplayNameUpdatedEvent`
   - `UpdateDisplayName_WhenNotRegistered_ReturnsError`
   - `UpdateDisplayName_WhenEmptyName_ReturnsError`

4. **Green**: Implement handler

5. Repeat for `SetOnlineStatusHandler`, `JoinChannelHandler`, `LeaveChannelHandler`

### Reducers

1. **Red**: Write tests for `UserRegisteredReducer`
   - `Reduce_UserRegistered_SetsAllProperties`

2. **Green**: Implement reducer

3. Repeat for all event reducers

## Files to Create

```
samples/Cascade/Cascade.Domain/
├── User/
│   ├── Commands/
│   │   ├── RegisterUser.cs
│   │   ├── UpdateDisplayName.cs
│   │   ├── SetOnlineStatus.cs
│   │   ├── JoinChannel.cs
│   │   └── LeaveChannel.cs
│   ├── Events/
│   │   ├── UserRegistered.cs
│   │   ├── DisplayNameUpdated.cs
│   │   ├── UserWentOnline.cs
│   │   ├── UserWentOffline.cs
│   │   ├── UserJoinedChannel.cs
│   │   └── UserLeftChannel.cs
│   ├── Handlers/
│   │   ├── RegisterUserHandler.cs
│   │   ├── UpdateDisplayNameHandler.cs
│   │   ├── SetOnlineStatusHandler.cs
│   │   ├── JoinChannelHandler.cs
│   │   └── LeaveChannelHandler.cs
│   ├── Reducers/
│   │   ├── UserRegisteredReducer.cs
│   │   ├── DisplayNameUpdatedReducer.cs
│   │   ├── UserWentOnlineReducer.cs
│   │   ├── UserWentOfflineReducer.cs
│   │   ├── UserJoinedChannelReducer.cs
│   │   └── UserLeftChannelReducer.cs
│   ├── IUserAggregateGrain.cs
│   ├── UserAggregateGrain.cs
│   ├── UserBrook.cs
│   └── UserState.cs

samples/Cascade/Cascade.Domain.L0Tests/
└── User/
    ├── Handlers/
    │   ├── RegisterUserHandlerTests.cs
    │   ├── UpdateDisplayNameHandlerTests.cs
    │   ├── SetOnlineStatusHandlerTests.cs
    │   ├── JoinChannelHandlerTests.cs
    │   └── LeaveChannelHandlerTests.cs
    └── Reducers/
        ├── UserRegisteredReducerTests.cs
        └── ... (one per reducer)
```

## Notes

- User grain key: `UserId` (string, e.g., "user-abc123")
- `JoinChannel`/`LeaveChannel` commands are for tracking what the user sees in their sidebar
- Actual channel membership is also tracked in `Channel` aggregate for consistency

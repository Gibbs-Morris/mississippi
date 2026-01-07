# Definitive Grain Reference

Comprehensive reference of all Orleans grains in the Mississippi framework and samples, including their projects, key types, and key formats.

> **Generated**: This document provides the authoritative list of grains across the solution.

## Framework Grains

### Event Sourcing - Brooks

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `IBrookWriterGrain` | EventSourcing.Brooks | `BrookKey` | `{BrookName}\|{EntityId}` | `CASCADE.CHAT.CHANNEL\|abc123` |
| `IBrookReaderGrain` | EventSourcing.Brooks | `BrookKey` | `{BrookName}\|{EntityId}` | `CASCADE.CHAT.CHANNEL\|abc123` |
| `IBrookCursorGrain` | EventSourcing.Brooks | `BrookKey` | `{BrookName}\|{EntityId}` | `CASCADE.CHAT.CHANNEL\|abc123` |
| `IBrookAsyncReaderGrain` | EventSourcing.Brooks | `BrookAsyncReaderKey` | `{BrookName}\|{EntityId}\|{InstanceId}` | `CASCADE.CHAT.CHANNEL\|abc123\|a1b2c3d4...` |
| `IBrookSliceReaderGrain` | EventSourcing.Brooks | `BrookRangeKey` | `{BrookName}\|{EntityId}\|{Start}\|{Count}` | `CASCADE.CHAT.CHANNEL\|abc123\|0\|1000` |

### Event Sourcing - Aggregates

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `IAggregateGrain` | EventSourcing.Aggregates.Abstractions | `BrookKey` | `{BrookName}\|{EntityId}` | `CASCADE.CHAT.CHANNEL\|abc123` |
| `IGenericAggregateGrain<T>` | EventSourcing.Aggregates.Abstractions | `string` | `{EntityId}` | `abc123` |

### Event Sourcing - Snapshots

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `ISnapshotCacheGrain<T>` | EventSourcing.Snapshots.Abstractions | `SnapshotStreamKey` | `{BrookName}\|{SnapshotStorageName}\|{EntityId}\|{ReducersHash}` | `CASCADE.CHAT.CHANNEL\|ChannelProj\|abc123\|ab12cd34` |
| `ISnapshotPersisterGrain` | EventSourcing.Snapshots.Abstractions | `SnapshotStreamKey` | `{BrookName}\|{SnapshotStorageName}\|{EntityId}\|{ReducersHash}` | `CASCADE.CHAT.CHANNEL\|ChannelProj\|abc123\|ab12cd34` |

### Event Sourcing - UX Projections

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `IUxProjectionGrain<T>` | EventSourcing.UxProjections.Abstractions | `string` | `{EntityId}` | `abc123` |
| `IUxProjectionCursorGrain` | EventSourcing.UxProjections.Abstractions | `UxProjectionCursorKey` | `{BrookName}\|{EntityId}` | `CASCADE.CHAT.CHANNEL\|abc123` |
| `IUxProjectionVersionedCacheGrain<T>` | EventSourcing.UxProjections.Abstractions | `UxProjectionVersionedCacheKey` | `{BrookName}\|{EntityId}\|{Version}` | `CASCADE.CHAT.CHANNEL\|abc123\|42` |
| `IUxProjectionSubscriptionGrain` | EventSourcing.UxProjections.Abstractions | `string` | `{ConnectionId}` | `conn-xyz-789` |

### Event Sourcing - UX Projections SignalR

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `IUxProjectionNotificationGrain` | EventSourcing.UxProjections.SignalR | `string` | `{ProjectionType}\|{BrookName}\|{EntityId}` | `ChannelProj\|CASCADE.CHAT.CHANNEL\|abc123` |
| `IUxClientGrain` | EventSourcing.UxProjections.SignalR | `string` | `{HubName}:{ConnectionId}` | `ChatHub:conn-xyz-789` |
| `IUxGroupGrain` | EventSourcing.UxProjections.SignalR | `string` | `{HubName}:{GroupName}` | `ChatHub:room-42` |
| `IUxServerDirectoryGrain` | EventSourcing.UxProjections.SignalR | `string` | `{Constant}` | `default` |

### Viaduct (SignalR Backplane)

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `ISignalRClientGrain` | Viaduct.Abstractions | `string` | `{HubName}:{ConnectionId}` | `ChatHub:conn-xyz-789` |
| `ISignalRGroupGrain` | Viaduct.Abstractions | `string` | `{HubName}:{GroupName}` | `ChatHub:room-42` |
| `ISignalRServerDirectoryGrain` | Viaduct.Abstractions | `string` | `{Constant}` | `default` |

### Ripples (Real-Time Subscriptions)

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `IRippleSubscriptionGrain` | Ripples.Orleans | `string` | `{ConnectionId}` | `conn-xyz-789` |

## Sample Aggregate Grains

| Grain Interface | Project | BrookName | Key Format | Example |
| --------------- | ------- | --------- | ---------- | ------- |
| `ICounterAggregateGrain` | samples/Crescent/ConsoleApp | `CRESCENT.SAMPLE.COUNTER` | `{BrookName}\|{EntityId}` | `CRESCENT.SAMPLE.COUNTER\|counter-1` |
| `IChannelAggregateGrain` | samples/Cascade/Cascade.Domain | `CASCADE.CHAT.CHANNEL` | `{BrookName}\|{EntityId}` | `CASCADE.CHAT.CHANNEL\|general` |
| `IUserAggregateGrain` | samples/Cascade/Cascade.Domain | `CASCADE.CHAT.USER` | `{BrookName}\|{EntityId}` | `CASCADE.CHAT.USER\|user-123` |
| `IConversationAggregateGrain` | samples/Cascade/Cascade.Domain | `CASCADE.CHAT.CONVERSATION` | `{BrookName}\|{EntityId}` | `CASCADE.CHAT.CONVERSATION\|thread-456` |

## Key Type Summary

| Key Type | Project | Format | Max Length |
| -------- | ------- | ------ | ---------- |
| `BrookKey` | EventSourcing.Brooks.Abstractions | `{BrookName}\|{EntityId}` | 1024 |
| `BrookAsyncReaderKey` | EventSourcing.Brooks.Abstractions | `{BrookName}\|{EntityId}\|{InstanceId}` | 1024 |
| `BrookRangeKey` | EventSourcing.Brooks.Abstractions | `{BrookName}\|{EntityId}\|{Start}\|{Count}` | 1024 |
| `SnapshotStreamKey` | EventSourcing.Snapshots.Abstractions | `{BrookName}\|{SnapshotStorageName}\|{EntityId}\|{ReducersHash}` | 2048 |
| `SnapshotKey` | EventSourcing.Snapshots.Abstractions | `{BrookName}\|{EntityId}\|{Version}\|{SnapshotStorageName}\|{ReducersHash}` | 2048 |
| `UxProjectionCursorKey` | EventSourcing.UxProjections.Abstractions | `{BrookName}\|{EntityId}` | 2048 |
| `UxProjectionVersionedCacheKey` | EventSourcing.UxProjections.Abstractions | `{BrookName}\|{EntityId}\|{Version}` | 2048 |

## Notes

- All composite keys use `|` as separator except SignalR-related grains which use `:`
- Directory grains (`IUxServerDirectoryGrain`, `ISignalRServerDirectoryGrain`) are singletons with constant key `"default"`
- Subscription grains (`IUxProjectionSubscriptionGrain`, `IRippleSubscriptionGrain`) are keyed by SignalR connection ID
- Generic grains (`IGenericAggregateGrain<T>`, `IUxProjectionGrain<T>`) use simple entity ID as key; the type parameter provides context

# Definitive Grain Reference

Comprehensive reference of all Orleans grains in the Mississippi framework and samples, including their projects, key types, and key formats.

> **Last Updated**: January 2026
>
> **Max Key Length**: All key types enforce a maximum length of **4192 characters**.

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
| `IGenericAggregateGrain<T>` | EventSourcing.Aggregates.Abstractions | `string` | `{EntityId}` | `abc123` |
| Custom `IGrainWithStringKey` | *(application-defined)* | `string` | `{EntityId}` | `abc123` |

> **Note**: The `IAggregateGrain` interface has been removed. Use `IGenericAggregateGrain<T>` for generic aggregates or define custom grain interfaces implementing `IGrainWithStringKey`.

### Event Sourcing - Snapshots

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `ISnapshotCacheGrain<T>` | EventSourcing.Snapshots.Abstractions | `SnapshotKey` | `{BrookName}\|{EntityId}\|{Version}\|{SnapshotStorageName}\|{ReducersHash}` | `CASCADE.CHAT.CHANNEL\|abc123\|42\|ChannelProj\|ab12cd34` |
| `ISnapshotPersisterGrain` | EventSourcing.Snapshots.Abstractions | `SnapshotKey` | `{BrookName}\|{EntityId}\|{Version}\|{SnapshotStorageName}\|{ReducersHash}` | `CASCADE.CHAT.CHANNEL\|abc123\|42\|ChannelProj\|ab12cd34` |

### Event Sourcing - UX Projections

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `IUxProjectionGrain<T>` | EventSourcing.UxProjections.Abstractions | `string` | `{EntityId}` | `abc123` |
| `IUxProjectionCursorGrain` | EventSourcing.UxProjections.Abstractions | `UxProjectionCursorKey` | `{BrookName}\|{EntityId}` | `CASCADE.CHAT.CHANNEL\|abc123` |
| `IUxProjectionVersionedCacheGrain<T>` | EventSourcing.UxProjections.Abstractions | `UxProjectionVersionedCacheKey` | `{BrookName}\|{EntityId}\|{Version}` | `CASCADE.CHAT.CHANNEL\|abc123\|42` |
| `IUxProjectionSubscriptionGrain` | EventSourcing.UxProjections.Abstractions | `string` | `{ConnectionId}` | `conn-xyz-789` |

### Aqueduct (SignalR Backplane)

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `ISignalRClientGrain` | Aqueduct.Abstractions | `SignalRClientKey` | `{HubName}\|{ConnectionId}` | `ChatHub\|conn-xyz-789` |
| `ISignalRGroupGrain` | Aqueduct.Abstractions | `SignalRGroupKey` | `{HubName}\|{GroupName}` | `ChatHub\|room-42` |
| `ISignalRServerDirectoryGrain` | Aqueduct.Abstractions | `SignalRServerDirectoryKey` | `{Constant}` | `default` |

> **Note**: SignalR grains were moved from `EventSourcing.UxProjections.SignalR` to `Aqueduct`. All SignalR keys now use strongly-typed key types with `|` separator for consistency with other Mississippi keys.

### Ripples (Real-Time Subscriptions)

| Grain Interface | Project | Key Type | Key Format | Example |
| --------------- | ------- | -------- | ---------- | ------- |
| `IRippleSubscriptionGrain` | Ripples.Orleans | `string` | `{ConnectionId}` | `conn-xyz-789` |

## Sample Aggregate Grains

| Grain Interface | Project | BrookName | Key Format | Example |
| --------------- | ------- | --------- | ---------- | ------- |
| `ICounterAggregateGrain` | samples/Crescent/ConsoleApp | `CRESCENT.SAMPLE.COUNTER` | `{EntityId}` | `counter-1` |
| `IChannelAggregateGrain` | samples/Cascade/Cascade.Domain | `CASCADE.CHAT.CHANNEL` | `{EntityId}` | `general` |
| `IUserAggregateGrain` | samples/Cascade/Cascade.Domain | `CASCADE.CHAT.USER` | `{EntityId}` | `user-123` |
| `IConversationAggregateGrain` | samples/Cascade/Cascade.Domain | `CASCADE.CHAT.CONVERSATION` | `{EntityId}` | `thread-456` |

> **Note**: Sample aggregate grains are now keyed by `{EntityId}` only. The brook name is derived from the `[BrookName]` attribute at runtime.

## Key Type Summary

| Key Type | Project | Format | Max Length |
| -------- | ------- | ------ | ---------- |
| `AggregateKey` | EventSourcing.Aggregates.Abstractions | `{EntityId}` | 4192 |
| `BrookKey` | EventSourcing.Brooks.Abstractions | `{BrookName}\|{EntityId}` | 4192 |
| `BrookAsyncReaderKey` | EventSourcing.Brooks.Abstractions | `{BrookName}\|{EntityId}\|{InstanceId}` | 4192 |
| `BrookRangeKey` | EventSourcing.Brooks.Abstractions | `{BrookName}\|{EntityId}\|{Start}\|{Count}` | 4192 |
| `SnapshotStreamKey` | EventSourcing.Snapshots.Abstractions | `{BrookName}\|{SnapshotStorageName}\|{EntityId}\|{ReducersHash}` | 4192 |
| `SnapshotKey` | EventSourcing.Snapshots.Abstractions | `{BrookName}\|{EntityId}\|{Version}\|{SnapshotStorageName}\|{ReducersHash}` | 4192 |
| `UxProjectionKey` | EventSourcing.UxProjections.Abstractions | `{EntityId}` | 4192 |
| `UxProjectionCursorKey` | EventSourcing.UxProjections.Abstractions | `{BrookName}\|{EntityId}` | 4192 |
| `UxProjectionVersionedCacheKey` | EventSourcing.UxProjections.Abstractions | `{BrookName}\|{EntityId}\|{Version}` | 4192 |
| `UxProjectionNotificationKey` | EventSourcing.UxProjections.Abstractions | `{ProjectionType}\|{BrookName}\|{EntityId}` | 4192 |
| `SignalRClientKey` | Aqueduct.Abstractions | `{HubName}\|{ConnectionId}` | 4192 |
| `SignalRGroupKey` | Aqueduct.Abstractions | `{HubName}\|{GroupName}` | 4192 |
| `SignalRServerDirectoryKey` | Aqueduct.Abstractions | `{Name}` | 4192 |

## Key Hierarchy

```text
Simple entity ID keys
├── AggregateKey → {EntityId}
└── UxProjectionKey → {EntityId}

Brook-based keys
├── BrookKey (BrookName|EntityId)
│   ├── Used by: IBrookWriterGrain, IBrookCursorGrain, IBrookReaderGrain
│   ├── Extended by: BrookAsyncReaderKey (adds InstanceId)
│   │   └── Used by: IBrookAsyncReaderGrain
│   └── Extended by: BrookRangeKey (adds Start|Count)
│       └── Used by: IBrookSliceReaderGrain
├── UxProjectionCursorKey (BrookName|EntityId)
│   └── Used by: IUxProjectionCursorGrain
└── UxProjectionVersionedCacheKey (BrookName|EntityId|Version)
    └── Used by: IUxProjectionVersionedCacheGrain<T>

Snapshot keys
├── SnapshotStreamKey (BrookName|SnapshotStorageName|EntityId|ReducersHash)
└── SnapshotKey (BrookName|EntityId|Version|SnapshotStorageName|ReducersHash)
    └── Used by: ISnapshotCacheGrain<T>, ISnapshotPersisterGrain

SignalR keys (Aqueduct)
├── SignalRClientKey (HubName|ConnectionId) → ISignalRClientGrain
├── SignalRGroupKey (HubName|GroupName) → ISignalRGroupGrain
└── SignalRServerDirectoryKey (Constant) → ISignalRServerDirectoryGrain

Connection-based keys
└── {ConnectionId} → IUxProjectionSubscriptionGrain, IRippleSubscriptionGrain
```

## Factory Methods

### IAggregateGrainFactory

```csharp
// For custom aggregate grain interfaces
TGrain GetAggregate<TGrain>(string entityId) where TGrain : IGrainWithStringKey;

// For generic aggregate pattern
IGenericAggregateGrain<TAggregate> GetGenericAggregate<TAggregate>(string entityId) where TAggregate : class;
IGenericAggregateGrain<TAggregate> GetGenericAggregate<TAggregate>(AggregateKey aggregateKey) where TAggregate : class;
```

### IBrookGrainFactory

```csharp
IBrookWriterGrain GetBrookWriterGrain(BrookKey key);
IBrookReaderGrain GetBrookReaderGrain(BrookKey key);
IBrookCursorGrain GetBrookCursorGrain(BrookKey key);
IBrookAsyncReaderGrain GetBrookAsyncReaderGrain(BrookKey key);
IBrookSliceReaderGrain GetBrookSliceReaderGrain(BrookRangeKey key);
```

### IUxProjectionGrainFactory

```csharp
IUxProjectionGrain<TProjection> GetUxProjectionGrain<TProjection>(string entityId);
IUxProjectionCursorGrain GetUxProjectionCursorGrain<TProjection>(string entityId);
IUxProjectionVersionedCacheGrain<TProjection> GetUxProjectionVersionedCacheGrain<TProjection>(string entityId, long version);
```

### ISnapshotGrainFactory

```csharp
ISnapshotCacheGrain<TSnapshot> GetSnapshotCacheGrain<TSnapshot>(SnapshotKey key);
ISnapshotPersisterGrain GetSnapshotPersisterGrain(SnapshotKey key);
```

## Notes

- All composite keys use `|` (pipe) as separator across the entire framework
- Directory grains (`ISignalRServerDirectoryGrain`) are singletons with constant key `"default"`
- Subscription grains (`IUxProjectionSubscriptionGrain`, `IRippleSubscriptionGrain`) are keyed by SignalR connection ID
- Generic grains (`IGenericAggregateGrain<T>`, `IUxProjectionGrain<T>`) use simple entity ID as key; the type parameter provides context
- Brook name is derived from `[BrookName]` attribute on aggregate/projection types at runtime
- Maximum key length is 4192 characters for all key types (Orleans grain key limit)

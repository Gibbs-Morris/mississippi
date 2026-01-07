# Grain Key Formats

This document maps every Orleans grain in the Mississippi solution together with its key type, string format, and how the key is constructed. The goal is to highlight the current state so we can decide on a consistent evolution plan.

> **Status**: Breaking changes are acceptable as the framework is not yet released.

## Standard Key Component Order

All composite keys follow a **consistent ordering principle**:

1. **BrookName** – Always first (identifies the event stream)
2. **EntityId** – Second (identifies the instance within the brook)
3. **Version** – Third, if applicable (identifies the point-in-time)
4. **Additional context** – Last (storage name, hash, instance ID, etc.)

This order ensures:

- Keys can be prefix-matched for routing and partitioning
- The most significant identifier (brook) comes first
- Version-specific keys extend non-versioned keys naturally

## Summary Table

| Grain Interface | Key Type | Format | Example | Factory Method |
| ----------------- | ---------- | ------------------------------ | ------------------- | --------------- |
| `IBrookWriterGrain` | `BrookKey` | `{BrookName}\|{EntityId}` | `CRESCENT.CHAT\|abc123` | `IBrookGrainFactory.GetBrookWriterGrain(BrookKey)` |
| `IBrookCursorGrain` | `BrookKey` | `{BrookName}\|{EntityId}` | `CRESCENT.CHAT\|abc123` | `IBrookGrainFactory.GetBrookCursorGrain(BrookKey)` |
| `IBrookReaderGrain` | `BrookKey` | `{BrookName}\|{EntityId}` | `CRESCENT.CHAT\|abc123` | `IBrookGrainFactory.GetBrookReaderGrain(BrookKey)` |
| `IBrookAsyncReaderGrain` | `BrookAsyncReaderKey` | `{BrookName}\|{EntityId}\|{InstanceId}` | `CRESCENT.CHAT\|abc123\|a1b2...` | `IBrookGrainFactory.GetBrookAsyncReaderGrain(BrookKey)` |
| `IBrookSliceReaderGrain` | `BrookRangeKey` | `{BrookName}\|{EntityId}\|{Start}\|{Count}` | `CRESCENT.CHAT\|abc123\|0\|1000` | `IBrookGrainFactory.GetBrookSliceReaderGrain(BrookRangeKey)` |
| `IGenericAggregateGrain<T>` | `string` | `{EntityId}` | `abc123` | `IAggregateGrainFactory.GetGenericAggregate<T>(entityId)` ✅ |
| Custom `IGrainWithStringKey` | `string` | `{EntityId}` | `abc123` | `IAggregateGrainFactory.GetAggregate<TGrain>(entityId)` ✅ |
| `ISnapshotCacheGrain<T>` | `SnapshotKey` | `{BrookName}\|{EntityId}\|{Version}\|{SnapshotStorageName}\|{ReducersHash}` | `CRESCENT.CHAT\|abc123\|42\|ChatProj\|ab12` | `ISnapshotGrainFactory.GetSnapshotCacheGrain<T>(SnapshotKey)` |
| `ISnapshotPersisterGrain` | `SnapshotKey` | `{BrookName}\|{EntityId}\|{Version}\|{SnapshotStorageName}\|{ReducersHash}` | `CRESCENT.CHAT\|abc123\|42\|ChatProj\|ab12` | `ISnapshotGrainFactory.GetSnapshotPersisterGrain(SnapshotKey)` |
| `IUxProjectionGrain<T>` | `string` | `{EntityId}` | `abc123` | `IUxProjectionGrainFactory.GetUxProjectionGrain<T>(entityId)` ✅ |
| `IUxProjectionCursorGrain` | `UxProjectionCursorKey` | `{BrookName}\|{EntityId}` | `CRESCENT.CHAT\|abc123` | `IUxProjectionGrainFactory.GetUxProjectionCursorGrain<T>(entityId)` ✅ |
| `IUxProjectionVersionedCacheGrain<T>` | `UxProjectionVersionedCacheKey` | `{BrookName}\|{EntityId}\|{Version}` | `CRESCENT.CHAT\|abc123\|42` | `IUxProjectionGrainFactory.GetUxProjectionVersionedCacheGrain<T>(entityId, version)` ✅ |
| `IUxProjectionSubscriptionGrain` | `string` | `{ConnectionId}` | `conn-xyz-789` | `GrainFactory.GetGrain<...>(connectionId)` |
| `IRippleSubscriptionGrain` | `string` | `{ConnectionId}` | `conn-xyz-789` | `GrainFactory.GetGrain<...>(connectionId)` |
| `IUxProjectionNotificationGrain` | `UxProjectionNotificationKey` | `{ProjectionType}\|{BrookName}\|{EntityId}` | `ChatProj\|CRESCENT.CHAT\|abc123` | Direct `GetGrain<...>(key)` |
| `ISignalRClientGrain` | `SignalRClientKey` | `{HubName}\|{ConnectionId}` | `ChatHub\|conn-xyz-789` | Direct `GetGrain<...>(key)` |
| `ISignalRGroupGrain` | `SignalRGroupKey` | `{HubName}\|{GroupName}` | `ChatHub\|room-42` | Direct `GetGrain<...>(key)` |
| `ISignalRServerDirectoryGrain` | `SignalRServerDirectoryKey` | `{Constant}` | `default` | Direct `GetGrain<...>("default")` |

> **Note**: ✅ marks preferred factory methods that use `entityId` only (brook derived from `[BrookName]` attribute).
>
> **Max Key Length**: All keys have a maximum length of **4192 characters**.

---

## Property Rename Plan ✅ COMPLETED

The following properties were renamed for consistency:

| Type | Previous Property | Current Property | Status |
| ------ | ------------------ | -------------- | ------- |
| `BrookKey` | `Type` | `BrookName` | ✅ Done |
| `BrookKey` | `Id` | `EntityId` | ✅ Done |
| `SnapshotStreamKey` | `ProjectionType` | `SnapshotStorageName` | ✅ Done |
| `SnapshotStreamKey` | `ProjectionId` | `EntityId` | ✅ Done |
| `UxProjectionKey` | *(composite key)* | `EntityId` only | ✅ Done (simplified) |
| `AggregateKey` | *(composite key)* | `EntityId` only | ✅ Done (simplified) |

---

## New Key Types

### `UxProjectionCursorKey` (new)

Replaces `UxProjectionKey` for `IUxProjectionCursorGrain`.

```csharp
public readonly record struct UxProjectionCursorKey(string BrookName, string EntityId)
{
    public static implicit operator string(UxProjectionCursorKey key) => $"{key.BrookName}|{key.EntityId}";
}
```

- **Format**: `{BrookName}|{EntityId}`
- **Used by**: `IUxProjectionCursorGrain`
- **Rationale**: The cursor only needs brook + entity to subscribe to cursor events. Projection type is irrelevant.

### `UxProjectionVersionedCacheKey` (new)

Replaces `UxProjectionVersionedKey` for `IUxProjectionVersionedCacheGrain<T>`.

```csharp
public readonly record struct UxProjectionVersionedCacheKey(string BrookName, string EntityId, BrookPosition Version)
{
    public static implicit operator string(UxProjectionVersionedCacheKey key) 
        => $"{key.BrookName}|{key.EntityId}|{key.Version.Value}";
}
```

- **Format**: `{BrookName}|{EntityId}|{Version}`
- **Used by**: `IUxProjectionVersionedCacheGrain<T>`
- **Rationale**: The versioned cache needs brook + entity + version. Projection type comes from `TProjection`.

---

## Detailed Grain Key Breakdown

### 1. Brook Grains (EventSourcing.Brooks)

#### 1.1 `IBrookWriterGrain`

- **Key Type**: `BrookKey`
- **Current Format**: `{Type}|{Id}`
- **Components (current)**:
  - `Type` – The brook type/name (e.g., `CRESCENT.NEWMODEL.CHAT`).
  - `Id` – The entity identifier within the brook (e.g., `chat-abc123`).
- **Proposed Format**: `{BrookName}|{EntityId}` (rename `Type` → `BrookName`, `Id` → `EntityId`).
- **Construction**: `BrookKey.ForGrain<TGrain>(entityId)` or `new BrookKey(type, id)`.
- **Factory Method**: `IBrookGrainFactory.GetBrookWriterGrain(BrookKey)`.

#### 1.2 `IBrookCursorGrain`

- **Key Type**: `BrookKey`
- **Current Format**: `{Type}|{Id}`
- **Components**: Same as `IBrookWriterGrain`.
- **Proposed Format**: `{BrookName}|{EntityId}` (rename only).
- **Construction**: Same as the writer grain.
- **Factory Method**: `IBrookGrainFactory.GetBrookCursorGrain(BrookKey)`.

#### 1.3 `IBrookReaderGrain`

- **Key Type**: `BrookKey`
- **Current Format**: `{Type}|{Id}`
- **Components**: Same as `IBrookWriterGrain`.
- **Proposed Format**: `{BrookName}|{EntityId}` (rename only).
- **Note**: `[StatelessWorker]` grain that distributes batch reads across activations.
- **Construction**: Same as the writer grain.
- **Factory Method**: `IBrookGrainFactory.GetBrookReaderGrain(BrookKey)`.

#### 1.4 `IBrookAsyncReaderGrain`

- **Key Type**: `BrookAsyncReaderKey`
- **Current Format**: `{BrookKey.Type}|{BrookKey.Id}|{InstanceId}`
- **Components (current)**:
  - `BrookKey.Type` – The embedded brook type/name.
  - `BrookKey.Id` – The embedded entity identifier.
  - `InstanceId` – A freshly generated `Guid` to keep each streaming read tied to a single grain.
- **Proposed Format**: `{BrookName}|{EntityId}|{InstanceId}` (rename `Type` → `BrookName`, `Id` → `EntityId`).
- **Note**: Not `[StatelessWorker]`; each streaming call gets a unique grain to preserve `IAsyncEnumerable` enumerator state.
- **Construction**: `BrookAsyncReaderKey.Create(BrookKey)`.
- **Factory Method**: `IBrookGrainFactory.GetBrookAsyncReaderGrain(BrookKey)`.

#### 1.5 `IBrookSliceReaderGrain`

- **Key Type**: `BrookRangeKey`
- **Current Format**: `{Type}|{Id}|{Start}|{Count}`
- **Components (current)**:
  - `Type` – The brook type/name.
  - `Id` – The entity identifier.
  - `Start` – Inclusive start position of the slice.
  - `Count` – Number of events returned in the slice.
- **Proposed Format**: `{BrookName}|{EntityId}|{Start}|{Count}` (rename `Type` → `BrookName`, `Id` → `EntityId`).
- **Construction**: `BrookRangeKey.FromBrookCompositeKey(BrookKey, start, count)`.
- **Factory Method**: `IBrookGrainFactory.GetBrookSliceReaderGrain(BrookRangeKey)`.

---

### 2. Aggregate Grains (EventSourcing.Aggregates)

#### 2.1 `IGenericAggregateGrain<TAggregate>`

- **Key Type**: `string` (simple entity ID)
- **Format**: `{EntityId}`
- **Components**:
  - `EntityId` – The identity of the aggregate instance.
- **Brook Name**: Derived from the `[BrookName]` attribute on the `TAggregate` type.
- **Construction**: Just the entity ID string.
- **Factory Method**: `IAggregateGrainFactory.GetGenericAggregate<TAggregate>(entityId)` ✅ **Preferred**
- **Notes**: This is the preferred pattern for new aggregates. The grain executes commands via `IRootCommandHandler<TAggregate>`.

#### 2.2 Custom Aggregate Grain Interface Pattern

- **Key Type**: `string` (simple entity ID)
- **Format**: `{EntityId}`
- **Components**:
  - `EntityId` – The identity of the aggregate instance.
- **Brook Name**: Derived from the `[BrookName]` attribute on the grain interface.
- **Factory Method**: `IAggregateGrainFactory.GetAggregate<TGrain>(entityId)` ✅ **Preferred**
- **Notes**: Use this pattern when you need a custom aggregate grain interface with domain-specific
  methods (e.g., `ICounterAggregateGrain` with `IncrementAsync`, `DecrementAsync`).

> **Breaking Change**: The `IAggregateGrain` base interface and `GetAggregate<TGrain>(BrookKey)` overload
> have been removed. All aggregate grains are now keyed by entity ID only.

---

### 3. Snapshot Grains (EventSourcing.Snapshots)

#### 3.1 `ISnapshotCacheGrain<TSnapshot>`

- **Key Type**: `SnapshotKey`
- **Current Format**: `{brookName}|{projectionType}|{projectionId}|{reducersHash}|{version}`
- **Agreed Format**: `{BrookName}|{EntityId}|{Version}|{SnapshotStorageName}|{ReducersHash}`
- **Components (agreed)**:
  - `BrookName` – The brook name (position 1).
  - `EntityId` – The entity identifier (position 2).
  - `Version` – The snapshot version (position 3).
  - `SnapshotStorageName` – The snapshot storage name, from `SnapshotStorageNameHelper.GetStorageName<T>()` (position 4).
  - `ReducersHash` – Hash of the reducer set (position 5).
- **Property Renames**:
  - `SnapshotStreamKey.ProjectionType` → `SnapshotStorageName`
  - `SnapshotStreamKey.ProjectionId` → `EntityId`
- **Behavior**: Immutable cache grain storing snapshots for a specific version.
- **Construction**: `new SnapshotKey(new SnapshotStreamKey(brookName, snapshotStorageName, entityId, reducersHash), version)`.
- **Factory Method**: `ISnapshotGrainFactory.GetSnapshotCacheGrain<TSnapshot>(SnapshotKey)`.

#### 3.2 `ISnapshotPersisterGrain`

- **Key Type**: `SnapshotKey`
- **Current Format**: Same as the cache grain.
- **Agreed Format**: `{BrookName}|{EntityId}|{Version}|{SnapshotStorageName}|{ReducersHash}`
- **Components (agreed)**: Same as `ISnapshotCacheGrain<TSnapshot>`.
- **Behavior**: One-way persistence grain tied to the same key as the cache.
- **Construction**: Same `SnapshotKey` as the cache grain.
- **Factory Method**: `ISnapshotGrainFactory.GetSnapshotPersisterGrain(SnapshotKey)`.

---

### 4. UX Projection Grains (EventSourcing.UxProjections)

> **Call Flow Analysis**: The UX projection grain family operates as follows:
>
> 1. **`IUxProjectionGrain<TProjection>`** (entry point) receives a request via `GetAsync()`.
> 2. It reads `BrookName` from its `[BrookName]` attribute and `EntityId` from its primary key.
> 3. It calls **`IUxProjectionCursorGrain`** using `UxProjectionCursorKey(brookName, entityId)` to get the current brook position.
> 4. It then calls **`IUxProjectionVersionedCacheGrain<TProjection>`** with `UxProjectionVersionedCacheKey(brookName, entityId, version)`.
> 5. The versioned cache grain calls **`ISnapshotCacheGrain<TProjection>`** to load the snapshot.

#### 4.1 `IUxProjectionGrain<TProjection>`

- **Key Type**: Simple string (no composite key type)
- **Current Format**: `{ProjectionTypeName}|{BrookKey.Type}|{BrookKey.Id}`
- **Agreed Format**: `{EntityId}`
- **How it works**:
  - The grain is keyed by `EntityId` only.
  - On activation, the grain reads `BrookName` from its `[BrookName]` attribute via `BrookNameHelper.GetBrookNameFromGrain(GetType())`.
  - The grain constructs `UxProjectionCursorKey` and `UxProjectionVersionedCacheKey` from the attribute + primary key.
- **Behavior**: Stateless entry point for projection reads that routes through cursor and cache grains.
- **Construction**: `grainFactory.GetGrain<IUxProjectionGrain<T>>(entityId)`.
- **Factory Method**: `IUxProjectionGrainFactory.GetUxProjectionGrain<TProjection>(entityId)`.

#### 4.2 `IUxProjectionCursorGrain`

- **Key Type**: `UxProjectionCursorKey` *(new)*
- **Current Format**: `{ProjectionTypeName}|{BrookKey.Type}|{BrookKey.Id}`
- **Agreed Format**: `{BrookName}|{EntityId}`
- **Components**:
  - `BrookName` – The brook name.
  - `EntityId` – The entity identifier.
- **Rationale**: The cursor subscribes to brook cursor events. It only needs `BrookKey` to subscribe to the correct stream. The `ProjectionTypeName` component was never used after activation.
- **Behavior**: Tracks the latest brook position for a specific brook + entity.
- **Construction**: `new UxProjectionCursorKey(brookName, entityId)`.
- **Factory Method**: `IUxProjectionGrainFactory.GetUxProjectionCursorGrain(UxProjectionCursorKey)`.

#### 4.3 `IUxProjectionVersionedCacheGrain<TProjection>`

- **Key Type**: `UxProjectionVersionedCacheKey` *(new)*
- **Current Format**: `{ProjectionTypeName}|{BrookKey.Type}|{BrookKey.Id}|{Version}`
- **Agreed Format**: `{BrookName}|{EntityId}|{Version}`
- **Components**:
  - `BrookName` – The brook name.
  - `EntityId` – The entity identifier.
  - `Version` – The projection version.
- **Rationale**: On activation, the grain calls `ISnapshotCacheGrain<TProjection>` which requires:
  - `BrookName` – extracted from key.
  - `EntityId` – extracted from key.
  - `SnapshotStorageName` – derived from `SnapshotStorageNameHelper.GetStorageName<TProjection>()`.
  - `ReducersHash` – computed from `IRootReducer<TProjection>.GetReducerHash()`.
  - `Version` – extracted from key.
  
  The `ProjectionTypeName` is **not needed** because the grain type `IUxProjectionVersionedCacheGrain<TProjection>` already provides the projection type identity.
- **Behavior**: Stateless worker caching the projection at the specified version for reuse.
- **Construction**: `new UxProjectionVersionedCacheKey(brookName, entityId, version)`.
- **Factory Method**: `IUxProjectionGrainFactory.GetUxProjectionVersionedCacheGrain<TProjection>(UxProjectionVersionedCacheKey)`.

#### 4.4 `IUxProjectionSubscriptionGrain`

- **Key Type**: `string` (raw)
- **Format**: `{connectionId}`
- **Components**: SignalR `ConnectionId`.
- **Behavior**: Manages all projection subscriptions for the connection.
- **Construction**: Direct `ConnectionId` string.
- **Factory Method**: `GrainFactory.GetGrain<IUxProjectionSubscriptionGrain>(connectionId)`.

---

### 5. SignalR Grains (Viaduct)

> **Note**: SignalR grains were moved from `EventSourcing.UxProjections.SignalR` to `Viaduct` and `Viaduct.Abstractions`.
> All SignalR key types now use the `|` (pipe) separator for consistency with other Mississippi keys.

#### 5.1 `ISignalRClientGrain`

- **Key Type**: `SignalRClientKey`
- **Format**: `{HubName}|{ConnectionId}`
- **Components**:
  - `HubName` – SignalR hub name.
  - `ConnectionId` – SignalR connection identifier.
- **Separator**: `|` (pipe)
- **Max Length**: 4192 characters (combined)
- **Behavior**: Tracks a single SignalR connection for recovery and messaging.
- **Construction**: `new SignalRClientKey(hubName, connectionId)`.
- **Location**: `Mississippi.Viaduct.SignalRClientKey`

#### 5.2 `ISignalRGroupGrain`

- **Key Type**: `SignalRGroupKey`
- **Format**: `{HubName}|{GroupName}`
- **Components**:
  - `HubName` – SignalR hub name.
  - `GroupName` – SignalR group identifier.
- **Separator**: `|` (pipe)
- **Max Length**: 4192 characters (combined)
- **Behavior**: Maintains membership lists for a SignalR group.
- **Construction**: `new SignalRGroupKey(hubName, groupName)`.
- **Location**: `Mississippi.Viaduct.SignalRGroupKey`

#### 5.3 `ISignalRServerDirectoryGrain`

- **Key Type**: `SignalRServerDirectoryKey`
- **Format**: `{Constant}` (typically `"default"`)
- **Components**: Constant placeholder identifying the singleton grain.
- **Behavior**: Tracks active SignalR servers for failure detection.
- **Construction**: `SignalRServerDirectoryKey.Default` or `new SignalRServerDirectoryKey(name)`.
- **Location**: `Mississippi.Viaduct.SignalRServerDirectoryKey`

---

## Key Type Hierarchy

```text
BrookKey (BrookName|EntityId)
├── Used by: IBrookWriterGrain, IBrookCursorGrain, IBrookReaderGrain
├── Extended by: BrookAsyncReaderKey (adds InstanceId)
│   └── Used by: IBrookAsyncReaderGrain
└── Extended by: BrookRangeKey (adds Start|Count)
    └── Used by: IBrookSliceReaderGrain

SnapshotStreamKey (BrookName|EntityId|SnapshotStorageName|ReducersHash)
└── Extended by: SnapshotKey (adds Version → position 3)
    └── Format: BrookName|EntityId|Version|SnapshotStorageName|ReducersHash
    └── Used by: ISnapshotCacheGrain<T>, ISnapshotPersisterGrain

UxProjectionCursorKey (BrookName|EntityId)
└── Used by: IUxProjectionCursorGrain

UxProjectionVersionedCacheKey (BrookName|EntityId|Version)
└── Used by: IUxProjectionVersionedCacheGrain<T>

SignalR keys (Viaduct)
├── SignalRClientKey (HubName|ConnectionId) → ISignalRClientGrain
├── SignalRGroupKey (HubName|GroupName) → ISignalRGroupGrain
└── SignalRServerDirectoryKey (Constant) → ISignalRServerDirectoryGrain

Simple string keys
├── {EntityId} → IGenericAggregateGrain<T>, Custom aggregate grains, IUxProjectionGrain<T>
└── {ConnectionId} → IUxProjectionSubscriptionGrain, IRippleSubscriptionGrain
```

> **Max Key Length**: All key types enforce a maximum length of **4192 characters**.

---

## UX Projection Key Analysis

### Call Flow Diagram (Agreed)

```mermaid
sequenceDiagram
    participant Client
    participant UxProjectionGrain as IUxProjectionGrain<T>
    participant CursorGrain as IUxProjectionCursorGrain
    participant VersionedCacheGrain as IUxProjectionVersionedCacheGrain<T>
    participant SnapshotCacheGrain as ISnapshotCacheGrain<T>

    Client->>UxProjectionGrain: GetAsync()
    Note over UxProjectionGrain: Key: {EntityId}<br/>BrookName from [BrookName] attribute
    
    UxProjectionGrain->>UxProjectionGrain: brookName = BrookNameHelper.GetBrookNameFromGrain(GetType())
    
    UxProjectionGrain->>CursorGrain: GetPositionAsync()
    Note over CursorGrain: Key: {BrookName}|{EntityId}<br/>(UxProjectionCursorKey)
    CursorGrain-->>UxProjectionGrain: BrookPosition (e.g., 42)
    
    UxProjectionGrain->>VersionedCacheGrain: GetAsync()
    Note over VersionedCacheGrain: Key: {BrookName}|{EntityId}|{Version}<br/>(UxProjectionVersionedCacheKey)
    
    VersionedCacheGrain->>VersionedCacheGrain: snapshotStorageName = SnapshotStorageNameHelper.GetStorageName<T>()
    VersionedCacheGrain->>VersionedCacheGrain: reducersHash = RootReducer.GetReducerHash()
    
    VersionedCacheGrain->>SnapshotCacheGrain: GetStateAsync()
    Note over SnapshotCacheGrain: Key: {BrookName}|{EntityId}|{Version}|{SnapshotStorageName}|{ReducersHash}<br/>(SnapshotKey)
    SnapshotCacheGrain-->>VersionedCacheGrain: TProjection
    
    VersionedCacheGrain-->>UxProjectionGrain: TProjection
    UxProjectionGrain-->>Client: TProjection
```

### Key Requirements Analysis (Agreed)

| Grain | BrookName | EntityId | Version | SnapshotStorageName | ReducersHash | Key Format |
| ------- | ----------- | ---------- | --------- | --------------------- | -------------- | ------------ |
| `IUxProjectionGrain<T>` | from `[BrookName]` | ✅ key | - | - | - | `{EntityId}` |
| `IUxProjectionCursorGrain` | ✅ key | ✅ key | - | - | - | `{BrookName}\|{EntityId}` |
| `IUxProjectionVersionedCacheGrain<T>` | ✅ key | ✅ key | ✅ key | from `TProjection` | computed | `{BrookName}\|{EntityId}\|{Version}` |
| `ISnapshotCacheGrain<T>` | ✅ key | ✅ key | ✅ key | ✅ key | ✅ key | `{BrookName}\|{EntityId}\|{Version}\|{SnapshotStorageName}\|{ReducersHash}` |

### Analysis Summary (Agreed)

1. **`ProjectionTypeName` is dropped** from UX projection keys because:
   - The grain type `IUxProjectionGrain<TProjection>` already includes `TProjection` as a type parameter
   - Orleans grain types are fully qualified, so `IUxProjectionVersionedCacheGrain<OrderSummary>` and `IUxProjectionVersionedCacheGrain<OrderDetails>` are different grain types
   - The snapshot storage name is derived from `TProjection` via `SnapshotStorageNameHelper.GetStorageName<TProjection>()`

2. **`BrookName` is REQUIRED** in cursor and versioned cache keys because:
   - The cursor grain needs it to subscribe to brook cursor events
   - The versioned cache grain needs it to call the snapshot grain
   - It cannot be inferred from `TProjection` since multiple projections can consume the same brook

3. **Agreed key formats**:
   - `IUxProjectionGrain<T>`: `{EntityId}` (brook from `[BrookName]` attribute on grain class)
   - `IUxProjectionCursorGrain`: `{BrookName}|{EntityId}` (new `UxProjectionCursorKey` type)
   - `IUxProjectionVersionedCacheGrain<T>`: `{BrookName}|{EntityId}|{Version}` (new `UxProjectionVersionedCacheKey` type)

4. **Standard key order**: `BrookName` → `EntityId` → `Version` → Additional context

---

## Identified Inconsistencies and Issues

### 1. Separator Consistency ✅ RESOLVED

All Mississippi keys now use `|` (pipe) as the separator, including SignalR keys in Viaduct.

### 2. Key Component Naming ✅ RESOLVED

Property renames completed:

- `BrookKey.Type` → `BrookName`
- `BrookKey.Id` → `EntityId`
- `SnapshotStreamKey.ProjectionType` → `SnapshotStorageName`
- `SnapshotStreamKey.ProjectionId` → `EntityId`

### 3. SnapshotKey vs UxProjectionKey Ordering ✅ RESOLVED

Standard order established: `BrookName` → `EntityId` → `Version` → Additional context.

### 4. SignalR Key Types ✅ RESOLVED

SignalR grains now use strongly-typed key types (`SignalRClientKey`, `SignalRGroupKey`, `SignalRServerDirectoryKey`)
with proper validation, max length enforcement (4192), and consistent `|` separator.

### 5. BrookKey Exposed in Aggregate Factory ✅ RESOLVED

The `GetAggregate<TGrain>(BrookKey)` overload has been removed from `IAggregateGrainFactory`.
The factory now exposes only entity ID-based methods:

- `GetAggregate<TGrain>(string entityId)` – For custom aggregate interfaces
- `GetGenericAggregate<TAggregate>(string entityId)` – For generic aggregate pattern
- `GetGenericAggregate<TAggregate>(AggregateKey aggregateKey)` – For typed key convenience

---

## Decisions Log

| # | Decision | Status |
| --- | ---------- | -------- |
| 1 | Rename `BrookKey.Type` → `BrookName`, `BrookKey.Id` → `EntityId` | ✅ Done |
| 2 | Rename `SnapshotStreamKey.ProjectionType` → `SnapshotStorageName` | ✅ Done |
| 3 | Rename `SnapshotStreamKey.ProjectionId` → `EntityId` | ✅ Done |
| 4 | Drop `ProjectionTypeName` from UX projection keys | ✅ Done |
| 5 | Create `UxProjectionCursorKey` for `IUxProjectionCursorGrain` | ✅ Done |
| 6 | Create `UxProjectionVersionedCacheKey` for `IUxProjectionVersionedCacheGrain<T>` | ✅ Done |
| 7 | `IUxProjectionGrain<T>` keyed by `{EntityId}` only, brook from `[BrookName]` | ✅ Done |
| 8 | Aggregate grains keyed by `{EntityId}` only, brook from `[BrookName]` | ✅ Done |
| 9 | Standard key order: `BrookName` → `EntityId` → `Version` → Additional context | ✅ Done |
| 10 | Breaking changes acceptable (not yet released) | ✅ Agreed |
| 11 | Add `IGenericAggregateGrain<T>` for generic aggregate pattern | ✅ Done |
| 12 | Add `GetAggregate<TGrain>(entityId)` to `IAggregateGrainFactory` | ✅ Done |
| 13 | Remove `GetAggregate<TGrain>(BrookKey)` from `IAggregateGrainFactory` | ✅ Done |
| 14 | Remove `IAggregateGrain` base interface (replaced by pattern) | ✅ Done |
| 15 | Simplify `AggregateKey` to `{EntityId}` only | ✅ Done |
| 16 | Simplify `UxProjectionKey` to `{EntityId}` only | ✅ Done |
| 17 | Max key length: 4192 characters | ✅ Done |
| 18 | SignalR separator changed from `:` to `\|` | ✅ Done |
| 19 | SignalR grains moved to Viaduct project | ✅ Done |
| 20 | Create strongly-typed SignalR key types | ✅ Done |

---

## Implementation Tasks ✅ ALL COMPLETED

All planned code changes have been implemented:

### Phase 1: Property Renames ✅ COMPLETED

- [x] Rename `BrookKey.Type` → `BrookName` in `BrookKey.cs`
- [x] Rename `BrookKey.Id` → `EntityId` in `BrookKey.cs`
- [x] Update all usages of `BrookKey.Type` and `BrookKey.Id`
- [x] Rename `SnapshotStreamKey.ProjectionType` → `SnapshotStorageName`
- [x] Rename `SnapshotStreamKey.ProjectionId` → `EntityId`
- [x] Update all usages of `SnapshotStreamKey` properties

### Phase 2: New Key Types ✅ COMPLETED

- [x] Create `UxProjectionCursorKey` record struct in `EventSourcing.UxProjections.Abstractions`
- [x] Create `UxProjectionVersionedCacheKey` record struct in `EventSourcing.UxProjections.Abstractions`
- [x] Update `IUxProjectionCursorGrain` to use `UxProjectionCursorKey`
- [x] Update `IUxProjectionVersionedCacheGrain<T>` to use `UxProjectionVersionedCacheKey`
- [x] Simplify `UxProjectionKey` to `{EntityId}` only

### Phase 3: Grain Key Simplification ✅ COMPLETED

- [x] Update `IUxProjectionGrain<T>` to be keyed by `{EntityId}` only
- [x] Update `UxProjectionGrainBase` to read brook name from `[BrookName]` attribute
- [x] Update `UxProjectionGrainBase` to construct `UxProjectionCursorKey` and `UxProjectionVersionedCacheKey`
- [x] Simplify `AggregateKey` to `{EntityId}` only
- [x] Update factories to use new key types

### Phase 4: Snapshot Key Reorder ✅ COMPLETED

- [x] Update `SnapshotKey` to use new order: `{BrookName}|{EntityId}|{Version}|{SnapshotStorageName}|{ReducersHash}`
- [x] Update parsing logic in `FromString` method
- [x] Update all snapshot key tests

### Phase 5: Generic Aggregate Pattern ✅ COMPLETED

- [x] Create `IGenericAggregateGrain<TAggregate>` interface
- [x] Add `GetGenericAggregate<TAggregate>(entityId)` to `IAggregateGrainFactory`
- [x] Add `GetAggregate<TGrain>(entityId)` to `IAggregateGrainFactory`

### Phase 6: Remove BrookKey from Aggregate Factory ✅ COMPLETED

- [x] Remove `IAggregateGrain` base interface
- [x] Remove `GetAggregate<TGrain>(BrookKey)` from `IAggregateGrainFactory`
- [x] Update Samples to use `GetAggregate<TGrain>(entityId)` instead

### Phase 7: SignalR Consolidation ✅ COMPLETED

- [x] Create strongly-typed SignalR key types (`SignalRClientKey`, `SignalRGroupKey`, `SignalRServerDirectoryKey`)
- [x] Change SignalR separator from `:` to `|` for consistency
- [x] Move SignalR grains from `EventSourcing.UxProjections.SignalR` to `Viaduct`
- [x] Delete `EventSourcing.UxProjections.SignalR` project
- [x] Enforce max key length of 4192 characters on all key types

---

## File References

### Key Types

- [BrookKey.cs](../src/EventSourcing.Brooks.Abstractions/BrookKey.cs)
- [BrookRangeKey.cs](../src/EventSourcing.Brooks.Abstractions/BrookRangeKey.cs)
- [BrookAsyncReaderKey.cs](../src/EventSourcing.Brooks.Abstractions/BrookAsyncReaderKey.cs)
- [SnapshotKey.cs](../src/EventSourcing.Snapshots.Abstractions/SnapshotKey.cs)
- [SnapshotStreamKey.cs](../src/EventSourcing.Snapshots.Abstractions/SnapshotStreamKey.cs)
- [UxProjectionKey.cs](../src/EventSourcing.UxProjections.Abstractions/UxProjectionKey.cs) — Simplified to `{EntityId}` only
- [UxProjectionCursorKey.cs](../src/EventSourcing.UxProjections.Abstractions/UxProjectionCursorKey.cs)
- [UxProjectionVersionedCacheKey.cs](../src/EventSourcing.UxProjections.Abstractions/UxProjectionVersionedCacheKey.cs)
- [AggregateKey.cs](../src/EventSourcing.Aggregates.Abstractions/AggregateKey.cs) — Simplified to `{EntityId}` only
- [SignalRClientKey.cs](../src/Viaduct.Abstractions/Keys/SignalRClientKey.cs)
- [SignalRGroupKey.cs](../src/Viaduct.Abstractions/Keys/SignalRGroupKey.cs)
- [SignalRServerDirectoryKey.cs](../src/Viaduct.Abstractions/Keys/SignalRServerDirectoryKey.cs)

### Grain Interfaces

- [IGenericAggregateGrain.cs](../src/EventSourcing.Aggregates.Abstractions/IGenericAggregateGrain.cs)
- [ISignalRClientGrain.cs](../src/Viaduct.Abstractions/Grains/ISignalRClientGrain.cs)
- [ISignalRGroupGrain.cs](../src/Viaduct.Abstractions/Grains/ISignalRGroupGrain.cs)
- [ISignalRServerDirectoryGrain.cs](../src/Viaduct.Abstractions/Grains/ISignalRServerDirectoryGrain.cs)

### Factories

- [IAggregateGrainFactory.cs](../src/EventSourcing.Aggregates.Abstractions/IAggregateGrainFactory.cs)
- [AggregateGrainFactory.cs](../src/EventSourcing.Aggregates/AggregateGrainFactory.cs)
- [BrookGrainFactory.cs](../src/EventSourcing.Brooks/Factory/BrookGrainFactory.cs)
- [SnapshotGrainFactory.cs](../src/EventSourcing.Snapshots/SnapshotGrainFactory.cs)
- [UxProjectionGrainFactory.cs](../src/EventSourcing.UxProjections/UxProjectionGrainFactory.cs)

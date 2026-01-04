# Data Flow: Browser Component to Cosmos Database

This document visualizes the complete data flow in Mississippi from Blazor/browser components to Cosmos DB storage, including how grain keys route data through the system.

## Grain Key Reference

All grain keys follow the **standard ordering principle**: `{BrookName}|{EntityId}|{Version}|{Context}`

```mermaid
flowchart LR
    subgraph KeyFormat["Key Format: BrookName | EntityId | Version | Context"]
        K1["CRESCENT.CHAT"] --> K2["order-123"] --> K3["42"] --> K4["ChatProj|ab12"]
    end
    
    K1 -.- L1["Identifies event stream"]
    K2 -.- L2["Instance within brook"]
    K3 -.- L3["Point-in-time (if needed)"]
    K4 -.- L4["Storage name, hash, etc."]
```

### Grain Key Summary

| Grain | Key Type | Format | Stateless | ReadOnly | Example |
|-------|----------|--------|:---------:|:--------:|---------|
| `IBrookWriterGrain` | `BrookKey` | `{BrookName}\|{EntityId}` | | | `CRESCENT.CHAT\|order-123` |
| `IBrookReaderGrain` | `BrookKey` | `{BrookName}\|{EntityId}` | ⚡ | 📖 | `CRESCENT.CHAT\|order-123` |
| `IBrookCursorGrain` | `BrookKey` | `{BrookName}\|{EntityId}` | | 📖 | `CRESCENT.CHAT\|order-123` |
| `IUxProjectionGrain<T>` | `string` | `{EntityId}` | ⚡ | 📖 | `order-123` |
| `IUxProjectionCursorGrain` | `UxProjectionCursorKey` | `{BrookName}\|{EntityId}` | | 📖 | `CRESCENT.CHAT\|order-123` |
| `IUxProjectionVersionedCacheGrain<T>` | `UxProjectionVersionedCacheKey` | `{BrookName}\|{EntityId}\|{Version}` | ⚡ | 📖 | `CRESCENT.CHAT\|order-123\|42` |
| `ISnapshotCacheGrain<T>` | `SnapshotKey` | `{BrookName}\|{EntityId}\|{Version}\|{StorageName}\|{Hash}` | | 📖 | `CRESCENT.CHAT\|order-123\|42\|ChatProj\|ab12` |
| `IUxProjectionSubscriptionGrain` | `string` | `{ConnectionId}` | | 📖 | `conn-xyz-789` |
| `IUxGroupGrain` | `string` | `{HubName}:{GroupName}` | | | `ChatHub:room-42` |

> ⚡ = `[StatelessWorker]` — Multiple activations for parallel load distribution  
> 📖 = `[ReadOnly]` — Calls can interleave without acquiring grain lock

## Overview Flowchart

```mermaid
flowchart TB
    subgraph Browser["🌐 Browser / Blazor Client"]
        RC["RippleComponent‹T›<br/>Blazor base component"]
        CR["ClientRipple‹T›<br/>Reactive projection state"]
        SRC["SignalRRippleConnection<br/>Hub connection manager"]
    end

    subgraph ASPNet["🖥️ ASP.NET Core Server"]
        Controller["UxProjectionControllerBase<br/>HTTP GET endpoints"]
        Hub["UxProjectionHub<br/>SignalR hub"]
    end

    subgraph Orleans["⚙️ Orleans Grains"]
        subgraph Projection["Projection Layer"]
            ProjGrain["⚡📖 IUxProjectionGrain‹T›<br/>Key: EntityId"]
            CursorGrain["📖 IUxProjectionCursorGrain<br/>Key: BrookName｜EntityId"]
            VersionedCache["⚡📖 IUxProjectionVersionedCacheGrain‹T›<br/>Key: BrookName｜EntityId｜Version"]
        end

        subgraph Snapshot["Snapshot Layer"]
            SnapCache["📖 ISnapshotCacheGrain‹T›<br/>Key: BrookName｜EntityId｜Version｜Storage｜Hash"]
            SnapPersist["ISnapshotPersisterGrain‹T›<br/>Same key as SnapshotCache"]
            Reducer["IRootReducer‹T›<br/>Event → State"]
        end

        subgraph Brook["Brook Layer - Event Streams"]
            BrookWriter["IBrookWriterGrain<br/>Key: BrookName｜EntityId"]
            BrookReader["⚡📖 IBrookReaderGrain<br/>Key: BrookName｜EntityId"]
            BrookCursor["📖 IBrookCursorGrain<br/>Key: BrookName｜EntityId"]
        end

        subgraph SignalRGrains["SignalR Grains"]
            SubGrain["📖 IUxProjectionSubscriptionGrain<br/>Key: ConnectionId"]
            GroupGrain["IUxGroupGrain<br/>Key: HubName：GroupName"]
        end

        Stream[("Orleans Stream<br/>BrookCursorMovedEvent")]
    end

    subgraph Cosmos["🗄️ Cosmos DB"]
        EventStore[("Events Container<br/>EventDocument, CursorDocument")]
        SnapStore[("Snapshots Container<br/>SnapshotDocument")]
    end

    %% READ PATH
    RC -->|"1. Subscribe to IRipple"| CR
    CR -->|"2. HTTP GET /projection/{id}"| Controller
    Controller -->|"3. GetAsync(entityId)"| ProjGrain
    ProjGrain -->|"4. GetPositionAsync()"| CursorGrain
    ProjGrain -->|"5. GetAtVersionAsync(v)"| VersionedCache
    VersionedCache -->|"6. GetStateAsync()"| SnapCache
    SnapCache -->|"7a. ReadAsync()"| SnapStore
    SnapCache -->|"7b. If miss: ReadEventsAsync()"| BrookReader
    BrookReader -->|"8. Query events"| EventStore
    SnapCache -->|"9. Reduce events"| Reducer

    %% SIGNALR SUBSCRIPTION
    CR -->|"Subscribe(type, id)"| SRC
    SRC -->|"SignalR invoke"| Hub
    Hub -->|"SubscribeAsync()"| SubGrain
    SubGrain -->|"Join group"| GroupGrain

    %% NOTIFICATION PATH (dotted for updates)
    BrookWriter -.->|"Publish"| Stream
    Stream -.->|"Notify"| CursorGrain
    CursorGrain -.->|"Version changed"| SubGrain
    SubGrain -.->|"SendAllAsync()"| GroupGrain
    GroupGrain -.->|"ProjectionUpdated"| Hub
    Hub -.->|"SignalR push"| SRC
    SRC -.->|"OnVersionUpdated"| CR
    CR -.->|"Re-fetch + StateChanged"| RC

    %% WRITE PATH
    BrookWriter -->|"AppendEventsAsync()"| EventStore
    SnapPersist -->|"WriteAsync()"| SnapStore

    %% Styling
    classDef browser fill:#e1f5fe,stroke:#01579b
    classDef aspnet fill:#fff3e0,stroke:#e65100
    classDef orleans fill:#f3e5f5,stroke:#7b1fa2
    classDef cosmos fill:#e8f5e9,stroke:#2e7d32
    classDef stream fill:#fce4ec,stroke:#c2185b

    class RC,CR,SRC browser
    class Controller,Hub aspnet
    class ProjGrain,CursorGrain,VersionedCache,SnapCache,SnapPersist,Reducer,BrookWriter,BrookReader,BrookCursor,SubGrain,GroupGrain orleans
    class EventStore,SnapStore cosmos
    class Stream stream
```

## Read Path with Grain Keys

> **Scaling Legend:** ⚡ = `[StatelessWorker]` (multiple activations) | 📖 = `[ReadOnly]` (no grain lock)

```mermaid
sequenceDiagram
    participant Browser as 🌐 Browser
    participant HTTP as HTTP Controller
    participant Proj as ⚡📖 UxProjectionGrain<br/>Key: order-123
    participant Cursor as 📖 CursorGrain<br/>Key: CHAT|order-123
    participant Cache as ⚡📖 VersionedCacheGrain<br/>Key: CHAT|order-123|42
    participant Snap as 📖 SnapshotCacheGrain<br/>Key: CHAT|order-123|42|Proj|ab12
    participant Brook as ⚡📖 BrookReaderGrain<br/>Key: CHAT|order-123
    participant Cosmos as 🗄️ Cosmos DB

    Browser->>HTTP: GET /api/projection/order-123
    
    Note over HTTP,Proj: ⚡ Stateless: routes to any activation
    HTTP->>Proj: GetAsync("order-123") 📖
    
    Note over Proj,Cursor: 📖 ReadOnly: no lock contention
    Proj->>Cursor: GetPositionAsync() 📖<br/>Key: CRESCENT.CHAT|order-123
    Cursor-->>Proj: version = 42
    
    Note over Proj,Cache: ⚡ Stateless: parallel version caches
    Proj->>Cache: GetAtVersionAsync(42) 📖<br/>Key: CRESCENT.CHAT|order-123|42
    
    Note over Cache,Snap: 📖 ReadOnly on all read calls
    Cache->>Snap: GetStateAsync() 📖<br/>Key: CRESCENT.CHAT|order-123|42|ChatProj|ab12
    
    alt Snapshot exists
        Snap->>Cosmos: Read SnapshotDocument<br/>PartitionKey: CRESCENT.CHAT|order-123
        Cosmos-->>Snap: State at version 40
        Note over Snap,Brook: ⚡ Stateless reader scales reads
        Snap->>Brook: ReadEventsAsync(40, 42) 📖<br/>Key: CRESCENT.CHAT|order-123
        Brook->>Cosmos: Query EventDocuments<br/>PartitionKey: CRESCENT.CHAT|order-123
        Cosmos-->>Brook: Events 41, 42
        Brook-->>Snap: Delta events
        Snap->>Snap: Reduce(state, events)
    else Snapshot miss
        Snap->>Brook: ReadEventsAsync(0, 42) 📖
        Brook->>Cosmos: Query all events
        Cosmos-->>Brook: Events 1-42
        Snap->>Snap: Reduce(default, events)
    end
    
    Snap-->>Cache: Projection state
    Cache-->>Proj: Projection state
    Proj-->>HTTP: Projection + ETag (v42)
    HTTP-->>Browser: JSON response
```

## Write Path with Grain Keys

```mermaid
sequenceDiagram
    participant API as 🖥️ API
    participant Agg as AggregateGrain<br/>Key: order-123
    participant Snap as SnapshotCacheGrain<br/>Key: CHAT|order-123|41|Proj|ab12
    participant Handler as CommandHandler
    participant Writer as BrookWriterGrain<br/>Key: CHAT|order-123
    participant Cosmos as 🗄️ Cosmos DB<br/>PartitionKey: CHAT|order-123
    participant Stream as Orleans Stream<br/>StreamId: CHAT|order-123
    participant Cursor as CursorGrain<br/>Key: CHAT|order-123
    participant Sub as SubscriptionGrain<br/>Key: conn-xyz-789
    participant Hub as SignalR Hub
    participant Browser as 🌐 Browser

    API->>Agg: ExecuteAsync(command)<br/>Key: order-123
    
    Note over Agg,Snap: BrookName from [BrookName] attribute
    Agg->>Snap: GetStateAsync()
    Snap-->>Agg: Current state (v41)
    Agg->>Handler: Handle(command, state)
    Handler-->>Agg: New events[]
    
    Note over Agg,Writer: Same BrookKey used
    Agg->>Writer: AppendEventsAsync(events)<br/>Key: CRESCENT.CHAT|order-123
    
    Note over Writer,Cosmos: PartitionKey = BrookName|EntityId
    Writer->>Cosmos: TransactionalBatch<br/>PartitionKey: CRESCENT.CHAT|order-123
    Cosmos-->>Writer: Success (v42)
    
    Note over Writer,Stream: StreamId derived from BrookKey
    Writer->>Stream: Publish(BrookCursorMovedEvent)<br/>StreamId: CRESCENT.CHAT|order-123
    Writer-->>Agg: New position (42)
    
    Note over Stream,Browser: Notification Path (async)
    Stream-->>Cursor: BrookCursorMovedEvent
    Cursor->>Cursor: Update position cache
    Cursor->>Sub: NotifyVersionChanged()
    Sub->>Hub: SendToGroupAsync()
    Hub->>Browser: ProjectionUpdated(CHAT, order-123, 42)
    Browser->>Browser: Trigger HTTP refresh
```

## Real-time Notification Flow

```mermaid
flowchart LR
    subgraph Write["Write Operation"]
        W1["AppendEvents"] --> W2["Cosmos Write"]
        W2 --> W3["Publish Stream Event"]
    end

    subgraph Stream["Orleans Streaming"]
        S1[("BrookCursorUpdates<br/>Stream")]
    end

    subgraph Notify["Notification Chain"]
        N1["CursorGrain<br/><i>Updates position</i>"]
        N2["SubscriptionGrain<br/><i>Routes to connections</i>"]
        N3["GroupGrain<br/><i>SignalR group</i>"]
    end

    subgraph Client["Browser"]
        C1["SignalR Client"]
        C2["ClientRipple"]
        C3["RippleComponent"]
    end

    W3 --> S1
    S1 --> N1
    N1 --> N2
    N2 --> N3
    N3 -->|"ProjectionUpdated"| C1
    C1 --> C2
    C2 -->|"HTTP GET refresh"| C3
    C3 -->|"Re-render"| C3
```

## Key Components Summary

| Layer | Component | Key Format | Responsibility |
|-------|-----------|------------|----------------|
| **Browser** | `RippleComponent<T>` | N/A | Blazor base component with auto-subscription lifecycle |
| **Browser** | `ClientRipple<T>` | N/A | Manages HTTP fetch + SignalR subscription |
| **Browser** | `SignalRRippleConnection` | N/A | Hub connection with auto-reconnect |
| **ASP.NET** | `UxProjectionControllerBase` | Route: `{entityId}` | HTTP endpoints for projection queries |
| **ASP.NET** | `UxProjectionHub` | N/A | SignalR hub for real-time updates |
| **Orleans** | `IUxProjectionGrain<T>` | `{EntityId}` | Stateless entry point for projection reads |
| **Orleans** | `IUxProjectionCursorGrain` | `{BrookName}\|{EntityId}` | Tracks latest projection version |
| **Orleans** | `IUxProjectionVersionedCacheGrain<T>` | `{BrookName}\|{EntityId}\|{Version}` | Caches specific version state |
| **Orleans** | `ISnapshotCacheGrain<T>` | `{BrookName}\|{EntityId}\|{Version}\|{Storage}\|{Hash}` | Loads/rebuilds state from snapshots + events |
| **Orleans** | `IBrookWriterGrain` | `{BrookName}\|{EntityId}` | Appends events + publishes stream notifications |
| **Orleans** | `IBrookReaderGrain` | `{BrookName}\|{EntityId}` | Reads event batches from storage |
| **Orleans** | `IUxProjectionSubscriptionGrain` | `{ConnectionId}` | Per-connection subscription tracking |
| **Orleans** | `IUxGroupGrain` | `{HubName}:{GroupName}` | SignalR group membership |
| **Cosmos** | Events Container | PartitionKey: `{BrookName}\|{EntityId}` | Stores `EventDocument` and `CursorDocument` |
| **Cosmos** | Snapshots Container | PartitionKey: varies | Stores `SnapshotDocument` for fast reads |

## Grain Key Derivation Flow

```mermaid
flowchart TB
    subgraph Input["📥 Input"]
        EntityId["entityId: order-123"]
        Attr["[BrookName] attribute: CRESCENT.CHAT"]
    end

    subgraph Derived["🔑 Key Derivation"]
        BK["BrookKey<br/>{BrookName}|{EntityId}<br/>CRESCENT.CHAT|order-123"]
        CursorKey["UxProjectionCursorKey<br/>{BrookName}|{EntityId}<br/>CRESCENT.CHAT|order-123"]
        Version["version: 42"]
        VersionedKey["UxProjectionVersionedCacheKey<br/>{BrookName}|{EntityId}|{Version}<br/>CRESCENT.CHAT|order-123|42"]
        Storage["storageName: ChatProj<br/>reducersHash: ab12"]
        SnapKey["SnapshotKey<br/>{BrookName}|{EntityId}|{Version}|{Storage}|{Hash}<br/>CRESCENT.CHAT|order-123|42|ChatProj|ab12"]
    end

    subgraph Grains["⚙️ Grain Activation"]
        G1["BrookWriterGrain"]
        G2["BrookReaderGrain"]
        G3["CursorGrain"]
        G4["VersionedCacheGrain"]
        G5["SnapshotCacheGrain"]
    end

    EntityId --> BK
    Attr --> BK
    BK --> CursorKey
    CursorKey --> Version
    Version --> VersionedKey
    VersionedKey --> Storage
    Storage --> SnapKey

    BK --> G1
    BK --> G2
    CursorKey --> G3
    VersionedKey --> G4
    SnapKey --> G5
```

## Partition Key Strategy

```mermaid
flowchart LR
    subgraph Cosmos["Cosmos DB Partitioning"]
        PK["Partition Key = BrookName|EntityId"]
        
        subgraph Partition1["Partition: CRESCENT.CHAT|order-123"]
            E1["EventDocument (pos=1)"]
            E2["EventDocument (pos=2)"]
            E3["EventDocument (pos=3)"]
            C1["CursorDocument (id=cursor)"]
        end
        
        subgraph Partition2["Partition: CRESCENT.CHAT|order-456"]
            E4["EventDocument (pos=1)"]
            E5["EventDocument (pos=2)"]
            C2["CursorDocument (id=cursor)"]
        end
    end
    
    PK --> Partition1
    PK --> Partition2
```

All events for a single entity are co-located in the same partition, enabling transactional batch writes and efficient queries. The partition key format matches the `BrookKey` format used by Orleans grains.

## Orleans Stream Details

| Property | Value |
|----------|-------|
| Stream Name | `BrookCursorUpdates` |
| Stream ID | Derived from `BrookKey`: `{BrookName}\|{EntityId}` |
| Event Type | `BrookCursorMovedEvent` |
| Publisher | `BrookWriterGrain` (after successful append) |
| Subscriber | `UxProjectionCursorGrain` (updates position cache) |
| Purpose | Propagates write notifications to projection layer |

## Key Type Definitions

```csharp
// Brook layer - event stream identity
public readonly record struct BrookKey(string BrookName, string EntityId)
{
    public static implicit operator string(BrookKey key) => $"{key.BrookName}|{key.EntityId}";
}

// Projection layer - cursor tracking
public readonly record struct UxProjectionCursorKey(string BrookName, string EntityId)
{
    public static implicit operator string(UxProjectionCursorKey key) => $"{key.BrookName}|{key.EntityId}";
}

// Projection layer - version-specific cache
public readonly record struct UxProjectionVersionedCacheKey(string BrookName, string EntityId, BrookPosition Version)
{
    public static implicit operator string(UxProjectionVersionedCacheKey key) 
        => $"{key.BrookName}|{key.EntityId}|{key.Version.Value}";
}

// Snapshot layer - full state identity
public readonly record struct SnapshotKey(
    string BrookName, 
    string EntityId, 
    BrookPosition Version, 
    string SnapshotStorageName, 
    string ReducersHash)
{
    public static implicit operator string(SnapshotKey key) 
        => $"{key.BrookName}|{key.EntityId}|{key.Version.Value}|{key.SnapshotStorageName}|{key.ReducersHash}";
}
```

## See Also

- [grain-key-formats.md](grain-key-formats.md) - Complete grain key reference
- [grain-dependencies.md](grain-dependencies.md) - Grain dependency diagram
- [grain-read-write-paths.md](grain-read-write-paths.md) - Detailed read/write flows

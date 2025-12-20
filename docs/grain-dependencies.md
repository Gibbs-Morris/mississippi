# Grain Dependencies

The diagram combines each grain with its interface (they are 1:1) and shows how grains call or observe each other.

```mermaid
flowchart LR
  subgraph API
    UxApi["UxProjectionControllerBase (ASP.NET API)"]
  end

  subgraph Brooks
    BrookWriter["BrookWriterGrain / IBrookWriterGrain"]
    BrookCursor["BrookCursorGrain / IBrookCursorGrain"]
    BrookReader["BrookReaderGrain / IBrookReaderGrain"]
    BrookSlice["BrookSliceReaderGrain / IBrookSliceReaderGrain"]
  end

  subgraph Snapshots
    SnapshotCache["SnapshotCacheGrainBase / ISnapshotCacheGrain"]
    SnapshotPersister["SnapshotPersisterGrain / ISnapshotPersisterGrain"]
  end

  subgraph Aggregates
    Aggregate["AggregateGrainBase / IAggregateGrain"]
  end

  subgraph UX
    UxProjection["UxProjectionGrainBase / IUxProjectionGrain"]
    UxCursor["UxProjectionCursorGrain / IUxProjectionCursorGrain"]
    UxCache["UxProjectionVersionedCacheGrainBase / IUxProjectionVersionedCacheGrain"]
  end

  Aggregate --> BrookCursor
  Aggregate --> BrookWriter
  Aggregate --> SnapshotCache

  SnapshotCache --> BrookReader
  SnapshotCache --> SnapshotPersister

  BrookReader --> BrookCursor
  BrookReader --> BrookSlice

  UxProjection --> UxCursor
  UxProjection --> UxCache
  UxCache --> SnapshotCache
  UxApi --> UxProjection

  BrookWriter -. streams .-> BrookCursor
  BrookWriter -. streams .-> UxCursor

  class BrookReader,UxProjection,UxCache stateless
  classDef stateless stroke-dasharray: 4 2, stroke:#0b7, color:#0b7;
```

Notes:
- Snapshot cache grains can call other snapshot cache grains to reuse base snapshots when rebuilding state.
- Dashed edges represent stream-driven notifications rather than direct method calls.
- Nodes with dashed teal borders are marked `[StatelessWorker]` in code.
- The API layer calls into UX projection grains via `IUxProjectionGrainFactory`.

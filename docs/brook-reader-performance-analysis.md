# Brook Reader Performance Analysis

> Deep analysis of the `[StatelessWorker]` and `[ReadOnly]` interactions with brook reader grains,
> understanding what was happening under the hood, and documenting the eager cache design.

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [The Original Design (Before Fix)](#the-original-design-before-fix)
3. [What Broke: IAsyncEnumerable + StatelessWorker](#what-broke-iasyncenumerable--statelessworker)
4. [Call-by-Call Flow Diagrams](#call-by-call-flow-diagrams)
5. [What Was "Shared" That Caused the Problem](#what-was-shared-that-caused-the-problem)
6. [Current Architecture (After Fix)](#current-architecture-after-fix)
7. [Performance Impact Analysis](#performance-impact-analysis)
8. [Proposed Designs to Restore Parallelism](#proposed-designs-to-restore-parallelism)
9. [Recommendation](#recommendation)

---

## Namespace Map

- Mississippi.EventSourcing.Brooks.Reader: `BrookReaderGrain`, `BrookAsyncReaderGrain`, `BrookSliceReaderGrain`
- Mississippi.EventSourcing.Brooks.Cursor: `BrookCursorGrain`
- Mississippi.EventSourcing.Brooks.Writer: `BrookWriterGrain`
- Mississippi.EventSourcing.Brooks.Abstractions: keys and definitions shared by the reader/writer grains
- Mississippi.EventSourcing.Brooks.Factory: grain factory used by cache and projection layers

---

## Executive Summary

The `EnumerationAbortedException` was caused by a fundamental incompatibility between Orleans'
`[StatelessWorker]` attribute and `IAsyncEnumerable` grain method returns. The issue was **not**
the grain's business-logic state (cache, etc.), but the **enumerator state** that Orleans creates
via `AsyncEnumerableGrainExtension` to manage streaming across network calls.

### Key Findings

| Aspect | Original Design | Current Design |
| ------ | --------------- | -------------- |
| `BrookReaderGrain` | `[StatelessWorker]` - multiple activations | `[StatelessWorker]` batch reader; streaming handled by `BrookAsyncReaderGrain` (unique key per call, non-StatelessWorker) |
| `BrookSliceReaderGrain.ReadAsync` | Lazy cache, no `[ReadOnly]` | Eager cache on activation, `[ReadOnly]` restored |
| `BrookSliceReaderGrain` cache | Populated on first read (mutation) | Populated on `OnActivateAsync` (no read mutation) |
| Slice read behavior | Silent cache miss handling | Throws `InvalidOperationException` if position > cache |
| Enumerator state | Stored in random activation | Stored in owning activation |
| Streaming | **BROKEN** - `EnumerationAbortedException` | **WORKS** |

---

## The Original Design (Before Fix)

### Original `BrookReaderGrain` Behavior

```csharp
[StatelessWorker]  // <-- REMOVED
internal class BrookReaderGrain : IBrookReaderGrain, IGrainBase
{
    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(...)
    {
        // Orleans creates AsyncEnumerableGrainExtension to hold enumerator
        foreach (var slice in slices)
        {
            await foreach (var ev in sliceGrain.ReadAsync(...))
            {
                yield return ev;  // Each yield is a network round-trip
            }
        }
    }
}
```

### Original `BrookSliceReaderGrain` Behavior (Before Eager Cache)

```csharp
internal class BrookSliceReaderGrain : IBrookSliceReaderGrain, IGrainBase
{
    private ImmutableArray<BrookEvent> Cache { get; set; } = ImmutableArray<BrookEvent>.Empty;

    // [ReadOnly] was NOT possible because Cache mutation happened on read
    public async IAsyncEnumerable<BrookEvent> ReadAsync(...)
    {
        if (cacheStale)
        {
            Cache = await LoadFromStorage();  // MUTATION! Violated [ReadOnly]
        }
        foreach (var ev in Cache)
        {
            yield return ev;
        }
    }
}
```

### Current `BrookSliceReaderGrain` Behavior (Eager Cache on Activation)

```csharp
internal sealed class BrookSliceReaderGrain : IBrookSliceReaderGrain, IGrainBase
{
    private ImmutableArray<BrookEvent> Cache { get; set; } = ImmutableArray<BrookEvent>.Empty;

    public async Task OnActivateAsync(CancellationToken token)
    {
        // Cache loads ONCE on activation - no mutation during reads
        Cache = await LoadFromStorage(token);
    }

    [ReadOnly]  // Now safe - reads are pure
    public async IAsyncEnumerable<BrookEvent> ReadAsync(
        BrookPosition minReadFrom, BrookPosition maxReadTo, ...)
    {
        // Validate requested range is within cache
        if (maxReadTo > lastPositionOfCache)
        {
            throw new InvalidOperationException(
                $"Requested position {maxReadTo} exceeds cached range.");
        }
        
        foreach (var ev in Cache.Where(inRange))
        {
            yield return ev;
        }
    }
}
```

---

## What Broke: IAsyncEnumerable + StatelessWorker

### Orleans IAsyncEnumerable Architecture

When a grain method returns `IAsyncEnumerable<T>`, Orleans does NOT stream the entire result
in one network message. Instead:

```mermaid
sequenceDiagram
    participant Client
    participant Silo
    participant Extension as AsyncEnumerableGrainExtension
    participant Grain

    Client->>Silo: Call ReadEventsAsync()
    Silo->>Extension: StartEnumeration<T>(requestId, request)
    Extension->>Grain: InvokeImplementation()
    Note over Extension: Stores enumerator in<br/>Dictionary<Guid, EnumeratorState> _enumerators
    Extension-->>Silo: (Status=Element, Value=first_batch)
    Silo-->>Client: First batch

    Client->>Silo: MoveNextAsync()
    Silo->>Extension: MoveNext<T>(requestId)
    Note over Extension: Looks up enumerator by requestId<br/>in _enumerators dictionary
    Extension->>Grain: enumerator.MoveNextAsync()
    Extension-->>Silo: (Status=Element, Value=next_batch)
    Silo-->>Client: Next batch
```

**Critical insight**: The `AsyncEnumerableGrainExtension` is attached to a **specific grain activation**.
It holds the `IAsyncEnumerator` in a `Dictionary<Guid, EnumeratorState>` keyed by request ID.

### The StatelessWorker Problem

With `[StatelessWorker]`, Orleans creates multiple activations of the same grain:

```mermaid
flowchart TB
    subgraph "Silo"
        SW[StatelessWorkerGrainContext]
        A1[ActivationData #1<br/>+ AsyncEnumerableGrainExtension]
        A2[ActivationData #2<br/>+ AsyncEnumerableGrainExtension]
        A3[ActivationData #3<br/>+ AsyncEnumerableGrainExtension]
    end

    Client[Client]

    Client -->|"1. ReadEventsAsync()<br/>routed to #1"| A1
    A1 -->|"StartEnumeration stores<br/>enumerator in #1's dict"| A1
    Client -->|"2. MoveNextAsync()<br/>routed to #2 ❌"| A2
    A2 -->|"MoveNext looks up requestId<br/>NOT FOUND in #2's dict"| A2

    style A2 fill:#f88,stroke:#333
```

**Result**: `EnumerationAbortedException` with message:
> "Enumerator not found. This likely indicates that the remote grain was deactivated since
> enumeration begun or that the enumerator was idle for longer than the expiration period."

---

## Call-by-Call Flow Diagrams

### Scenario: Read 250 events from position 0-249 (slice size = 100)

#### Original Architecture (BROKEN)

```mermaid
sequenceDiagram
    participant App as Application
    participant BR1 as BrookReaderGrain #1
    participant BR2 as BrookReaderGrain #2
    participant Ext1 as Extension #1
    participant Ext2 as Extension #2
    participant SR as SliceReaderGrain

    App->>BR1: ReadEventsAsync(0, 249)
    Note over BR1: [StatelessWorker] routes to activation #1
    BR1->>Ext1: StartEnumeration(reqId=ABC)
    Note over Ext1: _enumerators[ABC] = new EnumeratorState

    BR1->>SR: ReadAsync(0, 99)
    SR-->>BR1: events 0-99
    BR1-->>Ext1: yield events 0-99
    Ext1-->>App: Batch 0-99

    Note over App: await foreach continues...

    App->>BR2: MoveNextAsync() [routed to #2!]
    Note over BR2: [StatelessWorker] routes to activation #2
    BR2->>Ext2: MoveNext(reqId=ABC)
    Note over Ext2: _enumerators[ABC] NOT FOUND!

    Ext2-->>App: EnumerationAbortedException ❌
```

#### Current Architecture (WORKING but serialized)

```mermaid
sequenceDiagram
    participant App as Application
    participant BR as BrookReaderGrain
    participant Ext as Extension
    participant SR0 as SliceReaderGrain[0]
    participant SR1 as SliceReaderGrain[100]
    participant SR2 as SliceReaderGrain[200]

    App->>BR: ReadEventsAsync(0, 249)
    Note over BR: Single activation (no StatelessWorker)
    BR->>Ext: StartEnumeration(reqId=ABC)
    Note over Ext: _enumerators[ABC] = enumerator

    BR->>SR0: ReadAsync(0, 99)
    Note over SR0: PopulateCacheFromBrookAsync (if needed)
    SR0-->>BR: events 0-99
    BR-->>Ext: yield events 0-99
    Ext-->>App: Batch 0-99

    Note over App: MoveNextAsync()

    App->>BR: MoveNextAsync()
    BR->>Ext: MoveNext(reqId=ABC)
    Note over Ext: _enumerators[ABC] ✓ FOUND

    BR->>SR1: ReadAsync(100, 199)
    Note over SR1: PopulateCacheFromBrookAsync (if needed)
    SR1-->>BR: events 100-199
    BR-->>Ext: yield events 100-199
    Ext-->>App: Batch 100-199

    Note over App: MoveNextAsync()

    App->>BR: MoveNextAsync()
    BR->>SR2: ReadAsync(200, 249)
    SR2-->>BR: events 200-249
    BR-->>Ext: yield events 200-249
    Ext-->>App: Batch 200-249, Completed ✓
```

---

## What Was "Shared" That Caused the Problem

### NOT the issue: BrookSliceReaderGrain.Cache

While the `Cache` property mutation disqualified `[ReadOnly]` on `ReadAsync`, it wasn't
causing the streaming failure. Each slice grain is keyed by its range, so there's no
cross-activation confusion.

### THE issue: AsyncEnumerableGrainExtension._enumerators

```csharp
// From Orleans source: Orleans.Core/Runtime/AsyncEnumerableGrainExtension.cs
internal sealed partial class AsyncEnumerableGrainExtension
{
    // THIS is the "shared" state that broke with StatelessWorker
    private readonly Dictionary<Guid, EnumeratorState> _enumerators = [];

    public ValueTask<(EnumerationResult, object)> MoveNext<T>(Guid requestId, ...)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(_enumerators, requestId);
        if (Unsafe.IsNullRef(ref entry))
        {
            // This happens when StatelessWorker routes to wrong activation!
            return new((EnumerationResult.MissingEnumeratorError, default));
        }
        // ...
    }
}
```

Each grain activation gets its **own** `AsyncEnumerableGrainExtension` instance. With
`[StatelessWorker]`, there can be N activations, each with their own `_enumerators` dictionary.
When `MoveNextAsync()` routes to a different activation, the enumerator isn't found.

### State Ownership Diagram

```mermaid
flowchart TB
    subgraph "StatelessWorker = Multiple Activations"
        subgraph "Activation #1"
            A1["BrookReaderGrain instance"]
            E1["AsyncEnumerableGrainExtension<br/>_enumerators: {ABC: enumerator}"]
        end
        subgraph "Activation #2"
            A2["BrookReaderGrain instance"]
            E2["AsyncEnumerableGrainExtension<br/>_enumerators: {} (empty!)"]
        end
        subgraph "Activation #3"
            A3["BrookReaderGrain instance"]
            E3["AsyncEnumerableGrainExtension<br/>_enumerators: {}"]
        end
    end

    Client[Client with requestId=ABC]
    Client -->|"Call 1: lands on #1"| E1
    Client -.->|"Call 2: routed to #2<br/>ABC not found!"| E2

    style E2 fill:#f88,stroke:#333
```

---

## Current Architecture (After Fix)

### Changes Made

| Component | Change | Reason |
| --------- | ------ | ------ |
| `BrookReaderGrain` | Retained `[StatelessWorker]` for batch reads | Parallel fan-out for batch loads while keeping streaming separate |
| `BrookAsyncReaderGrain` | Non-`[StatelessWorker]` streaming reader with unique keys | Keeps `IAsyncEnumerable` enumerators on a single activation |
| `BrookSliceReaderGrain` | Cache loads on `OnActivateAsync` | Eager population enables `[ReadOnly]` on reads |
| `IBrookSliceReaderGrain.ReadAsync` | Restored `[ReadOnly]` | Cache no longer mutates on read (loaded on activation) |
| `IBrookSliceReaderGrain.ReadBatchAsync` | Restored `[ReadOnly]` | Cache no longer mutates on read (loaded on activation) |
| `BrookSliceReaderGrain.ReadAsync` | Throws `InvalidOperationException` | If requested position exceeds cached range |
| `IBrookCursorGrain.GetLatestPositionConfirmedAsync` | Removed `[ReadOnly]` | Method updates `TrackedCursorPosition` |
| `IUxProjectionVersionedCacheGrain.GetAsync` | Restored `[ReadOnly]` | Cache loads on activation (versioned = immutable) |

### Current Flow

```mermaid
flowchart TB
    subgraph "Batch path (StatelessWorker)"
        BR["BrookReaderGrain<br/>[StatelessWorker]<br/>batch fan-out"]
    end

    subgraph "Streaming path (unique key per call)"
        BAR["BrookAsyncReaderGrain<br/>single activation per call key"]
    end

    subgraph "Slice Readers (cache on activation)"
        SR0["SliceReaderGrain[brook:0:100]<br/>Cache: events 0-99 [ReadOnly]"]
        SR1["SliceReaderGrain[brook:100:100]<br/>Cache: events 100-199 [ReadOnly]"]
        SR2["SliceReaderGrain[brook:200:50]<br/>Cache: events 200-249 [ReadOnly]"]
    end

    Client[Client]

    Client -->|"Batch read"| BR
    BR -->|"ReadBatchAsync [ReadOnly]"| SR0
    BR -->|"ReadBatchAsync [ReadOnly]"| SR1
    BR -->|"ReadBatchAsync [ReadOnly]"| SR2

    Client -.->|"Streaming read"| BAR
    BAR -->|"ReadAsync [ReadOnly]"| SR0
    BAR -->|"ReadAsync [ReadOnly]"| SR1
    BAR -->|"ReadAsync [ReadOnly]"| SR2

    style BR stroke:#0b7,stroke-dasharray:4 2
    style BAR stroke:#333
```

---

## Performance Impact Analysis

### What We Changed

#### 1. StatelessWorker on BrookReaderGrain

**Before**: Multiple concurrent readers could exist per silo, each processing different read requests.

**After**: `[StatelessWorker]` kept for batch reads; streaming uses `BrookAsyncReaderGrain` with a unique key per call so `MoveNextAsync` always hits the same activation.

**Impact Severity**: **BALANCED** – batch throughput restored via StatelessWorkers, streaming stability preserved via per-call activations.

#### 2. [ReadOnly] on BrookSliceReaderGrain.ReadAsync - RESTORED

**Original Problem**: Cache mutation on first read disqualified `[ReadOnly]`.

**Solution**: Cache now loads eagerly on `OnActivateAsync`. All subsequent reads are pure.

**Current**: `[ReadOnly]` is restored. Multiple concurrent `ReadAsync` calls can interleave.

**Tradeoff**: If a read requests a position beyond the cached range, an `InvalidOperationException` is thrown. Callers must ensure they request positions within the slice's known range.

**Impact Severity**: **RESOLVED** - no longer a bottleneck

### Bottleneck Comparison

```mermaid
flowchart LR
    subgraph "Original (BROKEN)"
        C1[Client 1] --> BRA[BrookReader A]
        C2[Client 2] --> BRB[BrookReader B]
        C3[Client 3] --> BRC[BrookReader C]
        BRA --> SR0A[Slice 0]
        BRB --> SR0B[Slice 0]
        BRC --> SR0C[Slice 0]
        note1[/"3 parallel<br/>activations"\]
    end

    subgraph "Current (WORKING)"
        C4[Client 1] --> BAR1[BrookAsyncReader #A]
        C5[Client 2] --> BAR2[BrookAsyncReader #B]
        C6[Client 3] --> BAR3[BrookAsyncReader #C]
        BAR1 --> SR0[Slice 0]
        BAR2 --> SR0
        BAR3 --> SR0
        note2[/"unique activation<br/>per call key"\]
    end

    style BRA fill:#8f8
    style BRB fill:#8f8
    style BRC fill:#8f8
    style BAR1 fill:#8f8
    style BAR2 fill:#8f8
    style BAR3 fill:#8f8

    style note1 fill:#9f9,stroke:none
    style note2 fill:#9f9,stroke:none
```

---

## Proposed Designs to Restore Parallelism

### Option 1: Batch-Only API + Parallel Slice Fan-out

**Concept**: Keep `IAsyncEnumerable` for external API but use batch calls internally with parallel fan-out.

```csharp
public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
    BrookPosition? readFrom = null,
    BrookPosition? readTo = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var slices = GetSliceReads(start.Value, end.Value, sliceSize);

    // Fan-out: kick off ALL slice reads in parallel
    var tasks = slices.Select(s =>
        BrookGrainFactory.GetBrookSliceReaderGrain(key)
            .ReadBatchAsync(s.First, s.Last, cancellationToken)
    ).ToList();

    // Process in order as they complete (or wait for each in sequence)
    foreach (var (slice, task) in slices.Zip(tasks))
    {
        var batch = await task;
        foreach (var ev in batch)
        {
            yield return ev;
        }
    }
}
```

**Pros**:

- Slice reads happen in parallel
- No need for `[StatelessWorker]` on reader grain
- Streaming still works (enumerator stays in one activation)

**Cons**:

- Batches must fit in memory
- Ordering must be maintained (slices complete in any order)

**Estimated Speedup**: 3-10x depending on slice count and I/O overlap

---

### Option 2: Separate Streaming Orchestrator from Parallelizable Workers

**Concept**: Split responsibilities - one grain owns the streaming, another handles parallel work.

```mermaid
flowchart TB
    subgraph "Streaming Layer (Single Activation)"
        BRO[BrookReadOrchestrator<br/>Owns IAsyncEnumerable]
    end

    subgraph "Parallel Workers [StatelessWorker]"
        W1[BrookBatchReader #1]
        W2[BrookBatchReader #2]
        W3[BrookBatchReader #3]
    end

    subgraph "Slice Caches"
        S0[SliceReaderGrain 0]
        S1[SliceReaderGrain 1]
        S2[SliceReaderGrain 2]
    end

    Client --> BRO
    BRO -->|"ReadBatchAsync (no streaming)"| W1
    BRO -->|"ReadBatchAsync"| W2
    BRO -->|"ReadBatchAsync"| W3
    W1 --> S0
    W2 --> S1
    W3 --> S2
```

**Pros**:

- Clear separation of concerns
- Workers can use `[StatelessWorker]` safely (batch API only)
- Streaming grain stays single-activation

**Cons**:

- Additional grain type
- More complex architecture

---

### Option 3: Pre-Populated Cache with [ReadOnly] Restored

**Concept**: Ensure cache is populated **before** the streaming grain is called, so `ReadAsync` never mutates.

```csharp
// New grain for cache warming
public interface IBrookSliceCacheWarmerGrain : IGrainWithStringKey
{
    Task WarmAsync(BrookRangeKey key);  // NOT [ReadOnly] - does the mutation
}

// Modified slice reader - now truly [ReadOnly]
public interface IBrookSliceReaderGrain : IGrainWithStringKey
{
    [ReadOnly]  // Safe! PopulateCache never called
    IAsyncEnumerable<BrookEvent> ReadAsync(BrookPosition min, BrookPosition max, ...);
}

// Orchestrator flow:
// 1. Warm all slice caches in parallel
// 2. Then stream from [ReadOnly] slices
```

**Pros**:

- Restores `[ReadOnly]` interleaving on slice reads
- Cache warming can be parallel

**Cons**:

- Two-phase read (warm then read)
- Complexity in cache validity tracking

---

### Option 4: Queue-Based Producer/Consumer

**Concept**: Streaming grain produces items to a channel; workers populate in parallel.

```csharp
public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(...)
{
    var channel = Channel.CreateBounded<BrookEvent>(1000);

    // Producer task: parallel slice reads write to channel
    var producerTask = Task.Run(async () =>
    {
        var sliceTasks = slices.Select(async slice =>
        {
            await foreach (var ev in sliceGrain.ReadAsync(...))
            {
                await channel.Writer.WriteAsync(ev);
            }
        });
        await Task.WhenAll(sliceTasks);
        channel.Writer.Complete();
    });

    // Consumer: yield from channel in order
    await foreach (var ev in channel.Reader.ReadAllAsync())
    {
        yield return ev;
    }

    await producerTask;
}
```

**Pros**:

- True streaming with parallel fetch
- Backpressure via bounded channel

**Cons**:

- Ordering is lost (events arrive out of slice order)
- Complex error handling

---

## Recommendation

### Short Term: Option 1 (Batch + Parallel Fan-out)

Implement parallel fan-out using `ReadBatchAsync` internally:

```csharp
// BrookReaderGrain.cs
public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(...)
{
    var slices = GetSliceReads(start.Value, end.Value, sliceSize);

    // Fan-out all batch requests in parallel
    var tasks = slices.Select(s =>
        BrookGrainFactory.GetBrookSliceReaderGrain(key)
            .ReadBatchAsync(s.First, s.Last, cancellationToken)
    ).ToArray();

    // Yield in order
    for (int i = 0; i < tasks.Length; i++)
    {
        var batch = await tasks[i];
        foreach (var ev in batch)
        {
            yield return ev;
        }
    }
}
```

**Why**:

- Minimal code change
- Immediate performance gain
- No new grain types needed
- Memory-bounded by slice size (already configurable)

### Medium Term: Consider Option 2

If read throughput becomes critical:

1. Introduce `BrookBatchReaderGrain` with `[StatelessWorker]` for parallel batch reads
2. Keep `BrookReaderGrain` as streaming orchestrator
3. Slice grains focus purely on caching

---

## Summary

| Issue | Root Cause | Fix | Performance Impact |
| ----- | ---------- | --- | ------------------ |
| `EnumerationAbortedException` | Orleans stores `IAsyncEnumerator` in activation-specific extension; `[StatelessWorker]` routes `MoveNext` to wrong activation | Removed `[StatelessWorker]` from `BrookReaderGrain` | Fixed streaming, serialized readers |
| `[ReadOnly]` violation | Lazy cache population in `ReadAsync` mutated state | Moved cache to `OnActivateAsync`; restored `[ReadOnly]` | Slice reads now interleavable |
| Cache miss handling | Silent fallback to storage on miss | Throws `InvalidOperationException` if position > cache | Explicit failure; callers must respect slice boundaries |
| Lost parallelism at reader level | Single activation + serialized methods | Use batch API with parallel fan-out | Restore parallel performance |

The core insight: **IAsyncEnumerable in Orleans requires stable activation because the enumerator
is stored in an activation-scoped grain extension, not in the grain's business state**.

The slice reader optimization: **Eager cache loading on activation enables `[ReadOnly]` on all
read methods, allowing concurrent reads to interleave within a slice grain**.
